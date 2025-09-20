using System;

namespace TimSortNet;

public readonly ref struct TimSortConfig
{
	/* The maximum number of entries in a MergeState's pending-runs stack.
 * This is enough to sort arrays of size up to about
 *     32 * phi ** MAX_MERGE_PENDING
 * where phi ~= 1.618.  85 is ridiculouslylarge enough, good for an array
 * with 2**64 elements.
 */
	public readonly int MaxMergePending = 85;
	/* When we get into galloping mode, we stay there until both runs win less
 * often than MIN_GALLOP consecutive times.  See listsort.txt for more info.
 */
	public readonly int MinGallop = 7;
	/* Avoid malloc for small temp arrays. */
	public readonly int MergeStateTempSize = 256;

	public TimSortConfig()
	{
	}
	
	public TimSortConfig(int maxMergePending, int minGallop, int mergeStateTempSize)
	{
		MaxMergePending = maxMergePending;
		MinGallop = minGallop;
		MergeStateTempSize = mergeStateTempSize;
	}
}
