namespace TimSortNet.Benchmarks;

public class IntComparer : IComparer<int>
{
	public int Compare(int x, int y) => x - y;
}
