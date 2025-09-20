

namespace TimSortNet.Tests;

public class TimSorterTests
{
    [Test]
    public void BinarySort()
    {
        Span<int> span = [3, 2, 1];
        TimSorter.BinarySort(span, 0, Comparer<int>.Default);
        span.ToArray().Should().BeEquivalentTo([1, 2, 3]);
    }

    [Test]
    public void BinarySortLonger()
    {
        Span<int> span = [3, 2, 1, 298345, 4509, 233, 88];
        TimSorter.BinarySort(span, 0, Comparer<int>.Default);
        span.ToArray().Should().BeEquivalentTo([1, 2, 3, 88, 233, 4509, 298345]);
    }

    [Test]
    public void BinarySortShouldWork()
    {
        Span<int> span = [3, 2, 1, 298345, 4509, 233, 88];
        TimSorter.BinarySort(span, 0, Comparer<int>.Default);
        span.ToArray().Should().BeEquivalentTo([1, 2, 3, 88, 233, 4509, 298345])
            .And.BeInAscendingOrder();
    }

    [Test]
    public void BinarySortShouldSortComplexData()
    {
        Span<int> span = [-624, -670, -471, -285, 509, -171, -865, -831, 199, 431, -673, -709, -25, 206, -613, -396, -513, 989, -491, 578, 248, -837, -145, 434, 981, 917, 913, -398, 114, -121, 621, -206, -360, 747, 642, 476, -926, 815, -245, 724, -571, -643];

        TimSorter.BinarySort(span, 0, Comparer<int>.Default);
        span.ToArray().Should().BeInAscendingOrder();
    }

    [Test]
    public void CountRunSingleElement()
    {
        Span<int> span = [3];
        TimSorter.CountRun(span, Comparer<int>.Default).Should().Be((1, false));

    }


    [Test]
    public void CountRunTriples()
    {
        Span<int> span = [3, 2, 1];
        TimSorter.CountRun(span, Comparer<int>.Default).Should().Be((3, true));

        span = [1, 2, 3];
        TimSorter.CountRun(span, Comparer<int>.Default).Should().Be((3, false));

        span = [1, 2, -1];
        TimSorter.CountRun(span, Comparer<int>.Default).Should().Be((2, false));

    }

    [Test]
    public void SortEmptyShouldNotDoAnything()
    {
        Span<int> span = [3];
        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());

        span = [];
        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
    }

    [Test]
    public void SortPairShouldWorkAndBeStable()
    {
        Span<int> span = [3, 2];
        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());

        span.ToArray().Should().BeEquivalentTo([2, 3]).And.BeInAscendingOrder();

        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());

        span.ToArray().Should().BeEquivalentTo([2, 3]).And.BeInAscendingOrder();
    }

    [Test]
    public void SortShouldWorkAndBeStable()
    {
        Span<int> span = [3, 2, 1, 298345, 4509, 233, 88];
        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
        span.ToArray().Should().BeEquivalentTo([1, 2, 3, 88, 233, 4509, 298345])
            .And.BeInAscendingOrder();

        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
        span.ToArray().Should().BeEquivalentTo([1, 2, 3, 88, 233, 4509, 298345])
            .And.BeInAscendingOrder();
    }

    [Test]
    public void SortShouldDoGalop()
    {
        Span<int> span = [1, 2, 3, 4, 5, 6, 7, 100, 1209, 3000, 5000, 10000];
        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
        span.ToArray().Should().BeEquivalentTo([1, 2, 3, 4, 5, 6, 7, 100, 1209, 3000, 5000, 10000])
            .And.BeInAscendingOrder();
    }

    [Test]
    public void SortShouldJustReverse()
    {
        Span<int> span = [10000, 5000, 3000, 1209, 100, 7, 6, 5, 4, 3, 2, 1];

        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
        span.ToArray().Should().BeEquivalentTo([1, 2, 3, 4, 5, 6, 7, 100, 1209, 3000, 5000, 10000])
            .And.BeInAscendingOrder();
    }

    [Test]
    public void SortShouldReverseAndGalop()
    {
        Span<int> span = [1, 2, 3, 4, 5, 6, 7, 100, 1209, 3000, 5000, 10000, 5000, 3000, 1209, 100, 7, 6, 5, 4, 3, 2, 1];

        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
        span.ToArray().Should().BeEquivalentTo([1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 100, 100, 1209, 1209, 3000, 3000, 5000, 5000, 10000])
            .And.BeInAscendingOrder();
    }

    [Test]
    public void SortShouldSortComplexData()
    {
        Span<int> span = [-624, -670, -471, -285, 509, -171, -865, -831, 199, 431, -673, -709, -25, 206, -613, -396, -513, 989, -491, 578, 248, -837, -145, 434, 981, 917, 913, -398, 114, -121, 621, -206, -360, 747, 642, 476, -926, 815, -245, 724, -571, -643];

        TimSorter.Sort(span, Comparer<int>.Default, new TimSortConfig());
        span.ToArray().Should().BeInAscendingOrder();
    }
    
    [Test]
    public void SortShouldSortLongData()
    {
        int[] values = [.. Enumerable.Range(1, 1000)];
        Random random = new(7);
		random.Shuffle(values);
        
        TimSorter.Sort(values, Comparer<int>.Default, new TimSortConfig());
        values.Should().BeInAscendingOrder();
    }
}
