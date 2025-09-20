using System;
using BenchmarkDotNet.Attributes;
using TimSortNet;

namespace TimSortNet.Benchmarks;

public class SortBenchmarks
{
	[Params(10, 1000, 10000)]
	public int N;

	public int[]? Values;

	[IterationSetup]
	public void IterationSetup()
	{
		Values = [.. Enumerable.Range(1, N)];
		Random random = new(7);
		random.Shuffle(Values);
	}

	[Benchmark, ]
	public void SystemArraySort() => Array.Sort(Values!);

	[Benchmark]
	public void SystemArraySortIComparer() => Array.Sort(Values!, Comparer<int>.Default);

	[Benchmark]
	public void TimSortIComparer() => TimSorter.Sort(Values!, Comparer<int>.Default, new TimSortConfig());

	[Benchmark]
	public void BinarySortIComparer() => TimSorter.BinarySort(Values!, 0, Comparer<int>.Default);
}



