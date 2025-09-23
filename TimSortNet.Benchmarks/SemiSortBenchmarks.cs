using BenchmarkDotNet.Attributes;

namespace TimSortNet.Benchmarks;

[MemoryDiagnoser(displayGenColumns: false)]
[HideColumns("Job", "Error", "StdDev", "Median", "RatioSD", "y")]
public class SemiSortBenchmarks
{
	[Params(1000, 10000, 100000, 1000000)]
	public int N;

	public int[]? Values;

	[IterationSetup]
	public void IterationSetup()
	{
		Values = [.. Enumerable.Range(1, N)];
		Random random = new(7);
		Span<int> span = Values;

		// this simulates mostly sorted array
		var change = span.Slice(N >> 2, N >> 4);
		random.Shuffle(change);

		change = span.Slice(N >> 1, N >> 4);
		random.Shuffle(change);
	}

	[Benchmark,]
	public void SystemArraySort() => Array.Sort(Values!);

	[Benchmark]
	public void SystemArraySortIComparer() => Array.Sort(Values!, Comparer<int>.Default);

	[Benchmark]
	public void SystemArraySortDelegate() => Array.Sort(Values!, static (x, y) => x - y);

	[Benchmark]
	public void MemoryExtensionSortIComparer() => MemoryExtensions.Sort<int, Comparer<int>>(Values!, Comparer<int>.Default);

	[Benchmark]
	public void TimSortIComparer() => TimSorter.Sort<int, Comparer<int>>(Values!, Comparer<int>.Default, new TimSortConfig());

	[Benchmark]
	public void BinarySortIComparer() => TimSorter.BinarySort<int, Comparer<int>>(Values!, 0, Comparer<int>.Default);
}



