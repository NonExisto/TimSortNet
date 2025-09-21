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

			span[l..start].CopyTo(span.Slice(l + 1, start - l));
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
 */
	public static void Sort<T>(Span<T> span, IComparer<T> comparer, TimSortConfig config)
	{
		if (span.Length < 2) return;
		using var ms = new MergeState<T>(comparer, config);
		var nremaining = span.Length;
		var lo = 0;
		var hi = span.Length;
		var minrun = MergeComputeMinRun(nremaining);
		do
		{
			var (n, descending) = CountRun(span[lo..hi], comparer);
			if (descending)
			{
				span.Slice(lo, n).Reverse();
			}
			if (n < minrun)
			{
				var force = nremaining <= minrun ? nremaining : minrun;
				BinarySort(span.Slice(lo, force), n, comparer);
				n = force;
			}
			/* Push run onto pending-runs stack, and maybe merge. */
			Debug.Assert(ms.N < config.MaxMergePending);
			ms.AddPendingBlock(lo, n);
			ms.MergeCollapse(in span);
			/* Advance to find next run. */
			lo += n;
			nremaining -= n;
		} while (nremaining > 0);

		Debug.Assert(lo == hi);
		ms.MergeForceCollapse(in span);
		Debug.Assert(ms.N == 1);
		Debug.Assert(ms.CheckNoPendingLeft(span));
	}
}


