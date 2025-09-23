using System.Buffers;
using System.Diagnostics;

namespace TimSortNet;

internal ref struct MergeState<T, TCompare> : IDisposable where TCompare : IComparer<T>
{
	public readonly TCompare Comparer;
	/* This controls when we get *into* galloping mode.  It's initialized
     * to MIN_GALLOP.  merge_lo and merge_hi tend to nudge it higher for
     * random data, and lower for highly structured data.
     */
	public int MinGallop;
	private readonly int MIN_GALLOP;
	private T[]? buffer;
	/* 'a' is temp storage to help with merges.  It contains room for
     * alloced entries.
     */
	public Span<T> TempA;/* may point to temp array below */

	/* A stack of n pending runs yet to be merged.  Run #i starts at
     * address base[i] and extends for len[i] elements.  It's always
     * true (so long as the indices are in bounds) that
     *
     *     pending[i].base + pending[i].len == pending[i+1].base
     *
     * so we could cut the storage for this, but it's a minor amount,
     * and keeping all the info explicit simplifies the code.
     */
	internal int N;
	private readonly PendingBlock[] Pending;

	

	/* 'a' points to this when possible, rather than muck with malloc. */
	private readonly Span<T> temparray;

	public MergeState(TCompare comparer, TimSortConfig config)
	{
		Comparer = comparer;
		MIN_GALLOP = MinGallop = config.MinGallop;
		TempA = temparray = new T[config.MergeStateTempSize];
		Pending = new PendingBlock[config.MaxMergePending];
	}

	public void MergeGetMem(int needed)
	{
		if (needed <= TempA.Length) return;
		/* Don't realloc!  That can cost cycles to copy the old data, but
     * we don't care what's in the block.
     */
		Dispose();

		buffer = ArrayPool<T>.Shared.Rent(needed);
		TempA = buffer;
	}

	/*
	Locate the proper position of key in a sorted vector; if the vector contains
	an element equal to key, return the position immediately to the left of
	the leftmost equal element.  [gallop_right() does the same except returns
	the position to the right of the rightmost equal element (if any).]

	"a" is a sorted vector with n elements, starting at a[0].  n must be > 0.

	"hint" is an index at which to begin the search, 0 <= hint < n.  The closer
	hint is to the final result, the faster this runs.

	The return value is the int k in 0..n such that

			a[k-1] < key <= a[k]

	pretending that *(a-1) is minus infinity and a[n] is plus infinity.  IOW,
	key belongs at index k; or, IOW, the first k elements of a should precede
	key, and the last n-k should follow key.

	See listsort.txt for info on the method.
	*/
	private readonly int GallopLeft(T key, in Span<T> span, int hint)
	{
		Debug.Assert(hint >= 0 && hint < span.Length);
		int a = hint;
		int lastofs = 0;
		int ofs = 1;
		if (Comparer.IsLowerThan(span[a], key))
		{
			/* a[hint] < key -- gallop right, until
      * a[hint + lastofs] < key <= a[hint + ofs]
      */
			int maxofs = span.Length - hint;/* &a[n-1] is highest */
			while (ofs < maxofs)
			{
				if (Comparer.IsLowerThan(span[a + ofs], key))
				{
					lastofs = ofs;
					ofs = (ofs << 1) + 1;
					if (ofs <= 0)                   /* int overflow */
						ofs = maxofs;
				}
				else                /* key <= a[hint + ofs] */
					break;
			}
			if (ofs > maxofs)
				ofs = maxofs;
			/* Translate back to offsets relative to &a[0]. */
			lastofs += hint;
			ofs += hint;
		}
		else
		{
			/* key <= a[hint] -- gallop left, until
         * a[hint - ofs] < key <= a[hint - lastofs]
         */
			int maxofs = hint + 1;             /* &a[0] is lowest */

			while (ofs < maxofs)
			{
				if (Comparer.IsLowerThan(span[a - ofs], key))
					break;
				/* key <= a[hint - ofs] */
				lastofs = ofs;
				ofs = (ofs << 1) + 1;
				if (ofs <= 0)               /* int overflow */
					ofs = maxofs;
			}
			if (ofs > maxofs)
				ofs = maxofs;
			/* Translate back to positive offsets relative to &a[0]. */
			int k = lastofs;
			lastofs = hint - ofs;
			ofs = hint - k;
		}
		a -= hint;
		Debug.Assert(-1 <= lastofs && lastofs < ofs && ofs <= span.Length);

		++lastofs;
		while (lastofs < ofs)
		{
			int m = lastofs + ((ofs - lastofs) >> 1);

			if (Comparer.IsLowerThan(span[a + m], key))
				lastofs = m + 1;              /* a[m] < key */
			else
				ofs = m;                    /* key <= a[m] */
		}
		Debug.Assert(lastofs == ofs);

		return ofs;
	}

	/*
	Exactly like gallop_left(), except that if key already exists in a[0:n],
	finds the position immediately to the right of the rightmost equal value.

	The return value is the int k in 0..n such that

			a[k-1] <= key < a[k]

	or -1 if error.

	The code duplication is massive, but this is enough different given that
	we're sticking to "<" comparisons that it's much harder to follow if
	written as one routine with yet another "left or right?" flag.
	*/
	private readonly int GallopRight(T key, in Span<T> span, int hint)
	{
		Debug.Assert(hint >= 0 && hint < span.Length);
		int a = hint;
		int lastofs = 0;
		int ofs = 1;
		if (Comparer.IsLowerThan(key, span[a]))
		{
			/* key < a[hint] -- gallop left, until
      * a[hint - ofs] <= key < a[hint - lastofs]
      */
			int maxofs = hint + 1;             /* &a[0] is lowest */
			while (ofs < maxofs)
			{
				if (Comparer.IsLowerThan(key, span[a - ofs]))
				{
					lastofs = ofs;
					ofs = (ofs << 1) + 1;
					if (ofs <= 0)                   /* int overflow */
						ofs = maxofs;
				}
				else                /* a[hint - ofs] <= key */
					break;
			}
			if (ofs > maxofs)
				ofs = maxofs;
			/* Translate back to positive offsets relative to &a[0]. */
			int k = lastofs;
			lastofs = hint - ofs;
			ofs = hint - k;
		}
		else
		{
			/* a[hint] <= key -- gallop right, until
			 * a[hint + lastofs] <= key < a[hint + ofs]
			*/
			int maxofs = span.Length - hint;             /* &a[n-1] is highest */
			while (ofs < maxofs)
			{
				if (Comparer.IsLowerThan(key, span[a + ofs]))
					break;
				/* a[hint + ofs] <= key */
				lastofs = ofs;
				ofs = (ofs << 1) + 1;
				if (ofs <= 0)               /* int overflow */
					ofs = maxofs;
			}
			if (ofs > maxofs)
				ofs = maxofs;
			/* Translate back to offsets relative to &a[0]. */
			lastofs += hint;
			ofs += hint;
		}
		a -= hint;

		Debug.Assert(-1 <= lastofs && lastofs < ofs && ofs <= span.Length);

		/* Now a[lastofs] <= key < a[ofs], so key belongs somewhere to the
     * right of lastofs but no farther right than ofs.  Do a binary
     * search, with invariant a[lastofs-1] <= key < a[ofs].
     */
		++lastofs;
		while (lastofs < ofs)
		{
			int m = lastofs + ((ofs - lastofs) >> 1);

			if (Comparer.IsLowerThan(key, span[a + m]))
				ofs = m;                    /* key < a[m] */
			else
				lastofs = m + 1;              /* a[m] <= key */
		}
		Debug.Assert(lastofs == ofs);             /* so a[ofs-1] <= key < a[ofs] */
		return ofs;
	}

	/* Merge the na elements starting at pa with the nb elements starting at pb
	 * in a stable way, in-place.  na and nb must be > 0, and pa + na == pb.
	 * Must also have that *pb < *pa, that pa[na-1] belongs at the end of the
	 * merge, and should have na <= nb.  See listsort.txt for more info.
	 * Return 0 if successful, -1 if error.
	 */
	private Unit MergeLo(Span<T> span, int na)
	{
		var pa = span[..na];
		var pb = span[na..];
		Debug.Assert(pb.Length > 0 && pa.Length > 0);
		MergeGetMem(pa.Length);

		pa.CopyTo(TempA);
		var dest = span;
		pa = TempA[..na];

		(dest, pb) = dest.CopyFromAndUpdate(pb, 1);

		if (pb.Length == 0) return Succeed(dest, pa);
		if (pa.Length == 1) return CopyB(dest, pa, pb);

		int min_gallop = MinGallop;
		for (; ; )
		{
			int acount = 0;          /* # of times A won in a row */
			int bcount = 0;          /* # of times B won in a row */

			/* Do the straightforward thing until (if ever) one run
			 * appears to win consistently.
			 */
			for (; ; )
			{
				Debug.Assert(pa.Length > 1 && pb.Length > 0);
				var k = Comparer.IsLowerThan(pb[0], pa[0]);
				if (k)
				{
					(dest, pb) = dest.CopyFromAndUpdate(pb, 1);

					++bcount;
					acount = 0;

					if (pb.Length == 0) return Succeed(dest, pa);
					if (bcount >= min_gallop) break;
				}
				else
				{
					(dest, pa) = dest.CopyFromAndUpdate(pa, 1);

					++acount;
					bcount = 0;

					if (pa.Length == 1) return CopyB(dest, pa, pb);
					if (acount >= min_gallop) break;
				}
			}
			/* One run is winning so consistently that galloping may
         * be a huge win.  So try that, and continue galloping until
         * (if ever) neither run appears to be winning consistently
         * anymore.
         */
			++min_gallop;
			do
			{
				Debug.Assert(pa.Length > 1 && pb.Length > 0);
				min_gallop -= min_gallop > 1 ? 1 : 0;
				MinGallop = min_gallop;
				var k = GallopRight(pb[0], in pa, 0);
				acount = k;
				if (k > 0)
				{
					(dest, pa) = dest.CopyFromAndUpdate(pa, k);

					if (pa.Length == 1) return CopyB(dest, pa, pb);
					/* na==0 is impossible now if the comparison
					 * function is consistent, but we can't assume
					 * that it is.
					 */
					if (pa.Length == 0) return Succeed(dest, pa);
				}
				(dest, pb) = dest.CopyFromAndUpdate(pb, 1);

				if (pb.Length == 0) return Succeed(dest, pa);

				k = GallopLeft(pa[0], in pb, 0);
				bcount = k;
				if (k > 0)
				{
					(dest, pb) = dest.CopyFromAndUpdate(pb, k);
					if (pb.Length == 0) return Succeed(dest, pa);
				}
				(dest, pa) = dest.CopyFromAndUpdate(pa, 1);

				if (pa.Length == 1) return CopyB(dest, pa, pb);
			} while (acount >= MIN_GALLOP || bcount >= MIN_GALLOP);
			++min_gallop;           /* penalize it for leaving galloping mode */
			MinGallop = min_gallop;
		}

		static Unit Succeed(Span<T> dest, Span<T> pa)
		{
			if (pa.Length > 0)
			{
				pa.CopyTo(dest);
			}
			return default;
		}

		static Unit CopyB(Span<T> dest, Span<T> pa, Span<T> pb)
		{
			Debug.Assert(pa.Length == 1 && pb.Length > 0);
			pb.CopyTo(dest);
			dest[pb.Length] = pa[0];

			return default;
		}
	}

	private Unit MergeHi(Span<T> span, int na)
	{
		var pa = span[..na];
		var pb = span[na..];
		Debug.Assert(pb.Length > 0 && pa.Length > 0);
		MergeGetMem(pb.Length);
		var dest = span;
		pb.CopyTo(TempA);
		pb = TempA[..pb.Length];

		(dest, pa) = dest.CopyFromBackAndUpdate(pa);
		if (pa.Length == 0) return Succeed(dest, pb);
		if (pb.Length == 1) return CopyA(dest, pa, pb);

		int min_gallop = MinGallop;

		for (; ; )
		{
			int acount = 0;          /* # of times A won in a row */
			int bcount = 0;          /* # of times B won in a row */

			/* Do the straightforward thing until (if ever) one run
			 * appears to win consistently.
			 */
			for (; ; )
			{
				Debug.Assert(pb.Length > 0 && pb.Length > 1);
				var k = Comparer.IsLowerThan(pb[^1], pa[^1]);
				if (k)
				{

					(dest, pa) = dest.CopyFromBackAndUpdate(pa);

					++acount;
					bcount = 0;

					if (pa.Length == 0) return Succeed(dest, pb);
					if (acount >= min_gallop) break;
				}
				else
				{
					(dest, pb) = dest.CopyFromBackAndUpdate(pb);

					++bcount;
					acount = 0;

					if (pb.Length == 1) return CopyA(dest, pa, pb);
					if (bcount >= min_gallop) break;
				}
			}

			/* One run is winning so consistently that galloping may
			 * be a huge win.  So try that, and continue galloping until
			 * (if ever) neither run appears to be winning consistently
			 * anymore.
			 */
			++min_gallop;
			do
			{
				Debug.Assert(pa.Length > 0 && pb.Length > 1);
				min_gallop -= min_gallop > 1 ? 1 : 0;
				MinGallop = min_gallop;
				var k = GallopRight(pb[^1], in pa, pa.Length - 1);

				k = pa.Length - k;
				acount = k;
				if (k > 0)
				{
					(dest, pa) = dest.CopyFromBackAndUpdate(pa, k);

					if (pa.Length == 0) return Succeed(dest, pb);
				}
				(dest, pb) = dest.CopyFromBackAndUpdate(pb);
				if (pb.Length == 1) return CopyA(dest, pa, pb);

				k = GallopLeft(pa[^1], in pb, pb.Length - 1);

				k = pb.Length - k;
				bcount = k;
				if (k > 0)
				{
					(dest, pb) = dest.CopyFromBackAndUpdate(pb, k);
					if (pb.Length == 1)
						return CopyA(dest, pa, pb);
					/* nb==0 is impossible now if the comparison
					 * function is consistent, but we can't assume
					 * that it is.
					 */
					if (pb.Length == 0)
						return Succeed(dest, pb);
				}
				(dest, pa) = dest.CopyFromBackAndUpdate(pa);
				if (pa.Length == 0)
					return Succeed(dest, pb);
			} while (acount >= MIN_GALLOP || bcount >= MIN_GALLOP);
			++min_gallop;           /* penalize it for leaving galloping mode */
			MinGallop = min_gallop;
		}

		static Unit Succeed(Span<T> dest, Span<T> pb)
		{
			if (pb.Length > 0)
			{
				pb.CopyTo(dest);
			}
			return default;
		}

		static Unit CopyA(Span<T> dest, Span<T> pa, Span<T> pb)
		{
			Debug.Assert(pb.Length == 1 && pa.Length > 0);
			/* The first element of pb belongs at the front of the merge. */
			pa.CopyTo(dest[1..]);
			dest[0] = pb[0];

			return default;
		}
	}

	/* Merge the two runs at stack indices i and i+1.
		 */
	private void MergeAt(in Span<T> span, int i)
	{
		Debug.Assert(N >= 2);
		Debug.Assert(i >= 0);
		Debug.Assert(i == N - 2 || i == N - 3);

		var na = Pending[i].Offset;
		var pa = span.Slice(Pending[i].Offset, Pending[i].Length);
		var pb = span.Slice(Pending[i + 1].Offset, Pending[i + 1].Length);

		Debug.Assert(pa.Length > 0 && pb.Length > 0);
		/* Record the length of the combined runs; if i is the 3rd-last
     * run now, also slide over the last run (which isn't involved
     * in this merge).  The current run i+1 goes away in any case.
     */
		Pending[i].Length = pa.Length + pb.Length;
		if (i == N - 3)
		{
			Pending[i + 1] = Pending[i + 2];
		}
		N--;
		var ka = GallopRight(pb[0], in pa, 0);
		pa = pa[ka..];
		na += ka;
		if (pa.Length == 0) return;

		/* Where does a end in b?  Elements in b after that can be
     * ignored (already in place).
     */
		var kb = GallopLeft(pa[^1], in pb, pb.Length - 1);
		if (kb <= 0) return;
		pb = pb[..kb];

		/* Merge what remains of the runs, using a temp array with
     * min(na, nb) elements.
     */

		if (pa.Length <= pb.Length)
		{
			MergeLo(span.Slice(na, pa.Length + pb.Length), pa.Length);
		}
		else
		{
			MergeHi(span.Slice(na, pa.Length + pb.Length), pa.Length);
		}
	}

	/* Examine the stack of runs waiting to be merged, merging adjacent runs
	 * until the stack invariants are re-established:
	 *
	 * 1. len[-3] > len[-2] + len[-1]
	 * 2. len[-2] > len[-1]
	 *
	 * See listsort.txt for more info.
	 */
	public void MergeCollapse(in Span<T> span)
	{
		PendingBlock[] p = Pending;
		while (N > 1)
		{
			var n = N - 2;

			if (n > 0 && p[n - 1].Length <= p[n].Length + p[n + 1].Length)
			{
				if (p[n - 1].Length < p[n + 1].Length) n--;
				MergeAt(in span, n);
			}
			else if (p[n].Length <= p[n + 1].Length) MergeAt(in span, n);
			else break;
		}
	}

	/* Regardless of invariants, merge all runs on the stack until only one
	 * remains.  This is used at the end of the mergesort.
	 */
	public void MergeForceCollapse(in Span<T> span)
	{
		PendingBlock[] p = Pending;
		while (N > 1)
		{
			var cn = N - 2;
			if (cn > 0 && p[cn - 1].Length < p[cn + 1].Length) cn--;
			MergeAt(in span, cn);

		}
	}

	public void AddPendingBlock(int offset, int length)
	{
		Pending[N++] = new PendingBlock(offset, length);
	}

	public void Dispose()
	{
		if (buffer != null)
		{
			ArrayPool<T>.Shared.Return(buffer);
			buffer = null;
		}
		TempA = temparray;
	}

	public readonly bool CheckNoPendingLeft(Span<T> span)
	{
		return Pending[0].Offset == 0 && span.Length == Pending[0].Length;
	}
}
