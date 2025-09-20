using System.Diagnostics;

namespace TimSortNet;

public class TimSorter
{
	/* binarysort is the best method for sorting small arrays: it does
		 few compares, but can do data movement quadratic in the number of
		 elements.
		 [lo, hi) is a contiguous slice of a list, and is sorted via
		 binary insertion.  This sort is stable.
		 On entry, must have lo <= start <= hi, and that [lo, start) is already
		 sorted (pass start == lo if you don't know!).
		 If islt() complains return -1, else 0.
		 Even in case of error, the output slice will be some permutation of
		 the input (nothing is lost or duplicated).
	*/
	public static void BinarySort<T>(Span<T> span, int start, IComparer<T> comparer)
	{
		Debug.Assert(start >= 0 && start < span.Length);
		if (start == 0) start++;
		for (; start < span.Length; start++)
		{
			var l = 0;
			var r = start;
			var pivot = span[r];

			Debug.Assert(l < r);
			/* Invariants:
         * pivot >= all in [lo, l).
         * pivot  < all in [r, start).
         * The second is vacuously true at the start.
      */
			do
			{
				var p = l + ((r - l) >> 1);
				if (comparer.IsLowerThan(pivot, span[p]))
				{
					r = p;
				}
				else
				{
					l = p + 1;
				}
			} while (l < r);
			Debug.Assert(l == r);
			/* The invariants still hold, so pivot >= all in [lo, l) and
           pivot < all in [l, start), so pivot belongs at l.  Note
           that if there are elements equal to pivot, l points to the
           first slot after them -- that's why this sort is stable.
           Slide over to make room. 
			
			for (var p = start; p > l; p--)
			{
				span[p] = span[p - 1];
			}*/

			var len = start - l;
			span.Slice(l, len).CopyTo(span.Slice(l + 1, len));
			span[l] = pivot;
		}
	}

	/*
	Return the length of the run beginning at lo, in the slice [lo, hi).  lo < hi
	is required on entry.  "A run" is the longest ascending sequence, with

			lo[0] <= lo[1] <= lo[2] <= ...

	or the longest descending sequence, with

			lo[0] > lo[1] > lo[2] > ...

	Boolean *descending is set to 0 in the former case, or to 1 in the latter.
	For its intended use in a stable mergesort, the strictness of the defn of
	"descending" is needed so that the caller can safely reverse a descending
	sequence without violating stability (strict > ensures there are no equal
	elements to get out of order).

	Returns -1 in case of error.
	*/
	public static (int count, bool descending) CountRun<T>(Span<T> span, IComparer<T> comparer)
	{
		if (span.Length == 0) throw new ArgumentException("Span is empty");
		if (span.Length == 1) return (1, false);

		int lo = 1, n = 2;
		bool descending = false;
		if (comparer.IsLowerThan(span[lo], span[lo - 1]))
		{
			descending = true;
			for (++lo; lo < span.Length; ++lo, ++n)
			{
				if (!comparer.IsLowerThan(span[lo], span[lo - 1]))
					break;
			}
		}
		else
		{
			for (++lo; lo < span.Length; ++lo, ++n)
			{
				if (comparer.IsLowerThan(span[lo], span[lo - 1]))
					break;
			}

		}

		return (n, descending);
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

	Returns -1 on error.  See listsort.txt for info on the method.
	*/
	public static int GallopLeft<T>(T key, Span<T> span, int hint, IComparer<T> comparer)
	{
		Debug.Assert(hint >= 0 && hint < span.Length);
		int a = hint;
		int lastofs = 0;
		int ofs = 1;
		if (comparer.IsLowerThan(span[a], key))
		{
			/* a[hint] < key -- gallop right, until
      * a[hint + lastofs] < key <= a[hint + ofs]
      */
			int maxofs = span.Length - hint;/* &a[n-1] is highest */
			while (ofs < maxofs)
			{
				if (comparer.IsLowerThan(span[a + ofs], key))
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
				if (comparer.IsLowerThan(span[a - ofs], key))
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

			if (comparer.IsLowerThan(span[a + m], key))
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
	public static int GallopRight<T>(T key, Span<T> span, int hint, IComparer<T> comparer)
	{
		Debug.Assert(hint >= 0 && hint < span.Length);
		int a = hint;
		int lastofs = 0;
		int ofs = 1;
		if (comparer.IsLowerThan(key, span[a]))
		{
			/* key < a[hint] -- gallop left, until
      * a[hint - ofs] <= key < a[hint - lastofs]
      */
			int maxofs = hint + 1;             /* &a[0] is lowest */
			while (ofs < maxofs)
			{
				if (comparer.IsLowerThan(key, span[a - ofs]))
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
				if (comparer.IsLowerThan(key, span[a + ofs]))
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

			if (comparer.IsLowerThan(key, span[a + m]))
				ofs = m;                    /* key < a[m] */
			else
				lastofs = m + 1;              /* a[m] <= key */
		}
		Debug.Assert(lastofs == ofs);             /* so a[ofs-1] <= key < a[ofs] */
		return ofs;
	}

	/* Compute a good value for the minimum run length; natural runs shorter
	 * than this are boosted artificially via binary insertion.
	 *
	 * If n < 64, return n (it's too small to bother with fancy stuff).
	 * Else if n is an exact power of 2, return 32.
	 * Else return an int k, 32 <= k <= 64, such that n/k is close to, but
	 * strictly less than, an exact power of 2.
	 *
	 * See listsort.txt for more info.
	 */
	public static int MergeComputeMinRun(int n)
	{
		var r = 0; /* becomes 1 if any 1 bits are shifted off */
		Debug.Assert(n >= 0);
		while (n >= 64)
		{
			r |= n & 1;
			n >>= 1;
		}
		return n + r;

	}

/* An adaptive, stable, natural mergesort.  See listsort.txt.
 * Returns Py_None on success, NULL on error.  Even in case of error, the
 * list will be some permutation of its input state (nothing is lost or
 * duplicated).
 */
	public static void Sort<T>(Span<T> span, IComparer<T> comparer, TimSortConfig config)
	{
		if (span.Length < 2) return;
		using var ms = new MergeState<T>(comparer, config);
		var minrun = MergeComputeMinRun(span.Length);
		var nremaining = span.Length;
		var lo = 0;
		var hi = span.Length;
		do
		{
			var (n, descending) = CountRun(span, comparer);
			if (descending)
			{
				span.Slice(lo, n).Reverse();
			}
			if (n < minrun)
			{
				var force = nremaining <= minrun ? nremaining : minrun;
				BinarySort(span.Slice(lo, force), lo + n, comparer);
				n = force;
			}
			/* Push run onto pending-runs stack, and maybe merge. */
			Debug.Assert(ms.n < config.MaxMergePending);
			ms.AddPendingBlock(lo, n);
			ms.MergeCollapse(span);
			/* Advance to find next run. */
			lo += n;
			nremaining -= n;
		} while (nremaining > 0);

		Debug.Assert(lo == hi);
		ms.MergeForceCollapse(span);
		Debug.Assert(ms.n == 1);
		Debug.Assert(ms.CheckNoPendingLeft(span));
	}
}


