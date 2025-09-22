namespace TimSortNet;

internal struct PendingBlock
{
	public readonly int Offset;

	public int Length;

	public PendingBlock(int offset, int length) : this()
	{
		Offset = offset;
		Length = length;
	}
}
