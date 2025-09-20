using System.Buffers;
using System.Diagnostics;

namespace TimSortNet;

internal ref struct MergeState<T> : IDisposable
{
	public readonly IComparer<T> Comparer;
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
	internal int n;
	private readonly PendingBlock[] Pending;

	private struct PendingBlock
	{
		public int Offset;

		public int Length;

		public PendingBlock(int offset, int length) : this()
		{
			Offset = offset;
			Length = length;
		}
	}

	/* 'a' points to this when possible, rather than muck with malloc. */
	private readonly Span<T> temparray;

	public MergeState(IComparer<T> comparer, TimSortConfig config)
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
		pa = TempA;

		(dest, pb) = dest.CopyAndUpdate(pb, 1);

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
					(dest, pb) = dest.CopyAndUpdate(pb, 1);

					++bcount;
					acount = 0;

					if (pb.Length == 0) return Succeed(dest, pa);
					if (bcount >= min_gallop) break;
				}
				else
				{
					(dest, pa) = dest.CopyAndUpdate(pa, 1);

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
				var k = TimSorter.GallopRight(pb[0], pa, 0, Comparer);
				acount = k;
				if (k > 0)
				{
					(dest, pa) = dest.CopyAndUpdate(pa, k);

					if (pa.Length == 1) return CopyB(dest, pa, pb);
					/* na==0 is impossible now if the comparison
					 * function is consistent, but we can't assume
					 * that it is.
					 */
					if (pa.Length == 0) return Succeed(dest, pa);
				}
				(dest, pb) = dest.CopyAndUpdate(pb, 1);

				if (pb.Length == 0) return Succeed(dest, pa);

				k = TimSorter.GallopLeft(pa[0], pb, 0, Comparer);
				bcount = k;
				if (k > 0)
				{
					(dest, pb) = dest.CopyAndUpdate(pb, k);
					if (pb.Length == 0) return Succeed(dest, pa);
				}
				dest[0] = pa[0];
				dest = dest[1..];
				pa = pa[1..];


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

		(dest, pa) = dest.CopyBackAndUpdate(pa);
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
				var k = Comparer.IsLowerThan(pb[0], pa[0]);
				if (k)
				{

					(dest, pa) = dest.CopyBackAndUpdate(pa);

					++acount;
					bcount = 0;

					if (pa.Length == 0) return Succeed(dest, pb);
					if (acount >= min_gallop) break;
				}
				else
				{
					(dest, pb) = dest.CopyBackAndUpdate(pb);

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
				var k = TimSorter.GallopRight(pb[^1], pa, pa.Length - 1, Comparer);

				k = pa.Length - k;
				acount = k;
				if (k > 0)
				{
					(dest, pa) = dest.CopyBackAndUpdate(pa, k);

					if (pa.Length == 0) return Succeed(dest, pb);
				}
				(dest, pb) = dest.CopyBackAndUpdate(pb);
				if (pb.Length == 1) return CopyA(dest, pa, pb);

				k = TimSorter.GallopLeft(pa[pa.Length - 1], pb, pb.Length - 1, Comparer);

				k = pb.Length - k;
				bcount = k;
				if (k > 0)
				{
					(dest, pb) = dest.CopyBackAndUpdate(pb, k);
					if (pb.Length == 1)
						return CopyA(dest, pa, pb);
					/* nb==0 is impossible now if the comparison
					 * function is consistent, but we can't assume
					 * that it is.
					 */
					if (pb.Length == 0)
						return Succeed(dest, pb);
				}
				(dest, pa) = dest.CopyBackAndUpdate(pa);
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
		 * Returns 0 on success, -1 on error.
		 */
	private void MergeAt(Span<T> span, int i)
	{
		Debug.Assert(n > 2);
		Debug.Assert(i >= 0);
		Debug.Assert(i == n - 2 || i == n - 3);

		var pa = span.Slice(Pending[i].Offset, Pending[i].Length);
		var pb = span.Slice(Pending[i + 1].Offset, Pending[i + 1].Length);

		Debug.Assert(pa.Length > 0 && pb.Length > 0);
		/* Record the length of the combined runs; if i is the 3rd-last
     * run now, also slide over the last run (which isn't involved
     * in this merge).  The current run i+1 goes away in any case.
     */
		Pending[i].Length = pa.Length + pb.Length;
		if (i == n - 3)
		{
			span[i + 1] = span[i + 2];
		}
		n--;
		var k = TimSorter.GallopRight(pb[0], pa, 0, Comparer);
		pa = pa[k..];
		if (pa.Length == 0) return;

		/* Where does a end in b?  Elements in b after that can be
     * ignored (already in place).
     */
		k = TimSorter.GallopLeft(pa[^1], pb, pb.Length - 1, Comparer);
		if (k <= 0) return;

		/* Merge what remains of the runs, using a temp array with
     * min(na, nb) elements.
     */

		if (pa.Length <= k)
		{
			MergeLo(span.Slice(Pending[i].Offset, Pending[i].Length), pa.Length);
		}
		else
		{
			MergeHi(span.Slice(Pending[i].Offset, Pending[i].Length), pa.Length);
		}
	}

	/* Examine the stack of runs waiting to be merged, merging adjacent runs
	 * until the stack invariants are re-established:
	 *
	 * 1. len[-3] > len[-2] + len[-1]
	 * 2. len[-2] > len[-1]
	 *
	 * See listsort.txt for more info.
	 *
	 * Returns 0 on success, -1 on error.
	 */
	public void MergeCollapse(Span<T> span)
	{
		PendingBlock[] p = Pending;
		while (n > 1)
		{
			var cn = n - 2;

			if (cn > 0 && p[cn - 1].Length <= p[cn].Length + p[cn + 1].Length)
			{
				if (p[cn - 1].Length < p[cn + 1].Length) cn--;
				else MergeAt(span, cn);
			}
			else if (p[cn].Length <= p[cn + 1].Length) MergeAt(span, cn);
			else break;
		}
	}

	/* Regardless of invariants, merge all runs on the stack until only one
	 * remains.  This is used at the end of the mergesort.
	 *
	 * Returns 0 on success, -1 on error.
	 */
	public void MergeForceCollapse(Span<T> span)
	{
		PendingBlock[] p = Pending;
		while (n > 1)
		{
			var cn = n - 2;
			if (cn > 0 && p[cn - 1].Length < p[cn + 1].Length) cn--;
			else MergeAt(span, cn);

		}
	}

	public void AddPendingBlock(int offset, int length)
	{
		Pending[n++] = new MergeState<T>.PendingBlock(offset, length);
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
