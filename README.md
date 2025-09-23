# TimSortNet
A direct translation of python list sort algorithm (aka TimSort) written in C into modern C# using generics and Span&lt;T> to fully leverage its technical edge in terms of memory access pattern while providing fully safe implementation and a little configuration support too

## Scope
Whole idea of this repo is to create copy of existing C code and keep it as close at possible to original here:
http://svn.python.org/projects/python/trunk/Objects/listobject.c

Most relevant code comments are copied as is. They are referencing documentation at http://svn.python.org/projects/python/trunk/Objects/listsort.txt

## Usage
This project is code only, i.e. no nuget package will be available. To use it just clone it to your local machine and reference existing project from you current working solution. I really like Rust development approach on code reuse

## Benchmark

BenchmarkDotNet v0.15.3, Windows 11 (10.0.22631.5768/23H2/2023Update/SunValley3)
AMD Ryzen 7 PRO 4750U with Radeon Graphics 1.70GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.304
  [Host]     : .NET 9.0.8 (9.0.8, 9.0.825.36511), X64 RyuJIT x86-64-v3
  Job-CNUJVU : .NET 9.0.8 (9.0.8, 9.0.825.36511), X64 RyuJIT x86-64-v3

Job=Job-CNUJVU  InvocationCount=1  UnrollFactor=1

| Method                       | N       | Mean                | Allocated |
|----------------------------- |-------- |--------------------:|----------:|
| SystemArraySort              | 10      |            325.6 ns |         - |
| SystemArraySortIComparer     | 10      |            365.5 ns |         - |
| MemoryExtensionSortIComparer | 10      |            522.2 ns |         - |
| TimSortIComparer             | 10      |          2,065.9 ns |    1752 B |
| BinarySortIComparer          | 10      |          1,057.1 ns |         - |
| SystemArraySort              | 1000    |         19,775.8 ns |         - |
| SystemArraySortIComparer     | 1000    |         18,252.8 ns |         - |
| MemoryExtensionSortIComparer | 1000    |         20,212.2 ns |         - |
| TimSortIComparer             | 1000    |        266,848.8 ns |    1752 B |
| BinarySortIComparer          | 1000    |        108,405.9 ns |         - |
| SystemArraySort              | 10000   |        429,121.0 ns |         - |
| SystemArraySortIComparer     | 10000   |        394,156.7 ns |         - |
| MemoryExtensionSortIComparer | 10000   |        393,353.8 ns |         - |
| TimSortIComparer             | 10000   |      3,307,592.3 ns |    1752 B |
| BinarySortIComparer          | 10000   |      2,470,083.3 ns |         - |
| SystemArraySort              | 100000  |      4,940,992.3 ns |         - |
| SystemArraySortIComparer     | 100000  |      4,955,571.4 ns |         - |
| MemoryExtensionSortIComparer | 100000  |      4,946,350.0 ns |         - |
| TimSortIComparer             | 100000  |     19,469,292.3 ns |    1752 B |
| BinarySortIComparer          | 100000  |    225,854,373.3 ns |         - |
| SystemArraySort              | 1000000 |     57,438,061.5 ns |         - |
| SystemArraySortIComparer     | 1000000 |     57,905,160.0 ns |         - |
| MemoryExtensionSortIComparer | 1000000 |     58,013,900.0 ns |         - |
| TimSortIComparer             | 1000000 |    295,801,023.1 ns |    1752 B |
| BinarySortIComparer          | 1000000 | 21,790,511,513.3 ns |         - |

and for a partially sorted data

| Method                       | N       | Mean           | Allocated |
|----------------------------- |-------- |---------------:|----------:|
| SystemArraySort              | 1000    |       5.960 us |         - |
| SystemArraySortIComparer     | 1000    |       6.195 us |         - |
| SystemArraySortDelegate      | 1000    |      23.470 us |         - |
| MemoryExtensionSortIComparer | 1000    |       6.223 us |         - |
| TimSortIComparer             | 1000    |      29.553 us |    1752 B |
| BinarySortIComparer          | 1000    |      44.329 us |         - |
| SystemArraySort              | 10000   |      83.106 us |         - |
| SystemArraySortIComparer     | 10000   |      82.893 us |         - |
| SystemArraySortDelegate      | 10000   |     287.443 us |         - |
| MemoryExtensionSortIComparer | 10000   |      82.800 us |         - |
| TimSortIComparer             | 10000   |     385.664 us |    1752 B |
| BinarySortIComparer          | 10000   |     422.180 us |         - |
| SystemArraySort              | 100000  |   1,092.792 us |         - |
| SystemArraySortIComparer     | 100000  |   1,091.610 us |         - |
| SystemArraySortDelegate      | 100000  |   2,204.227 us |         - |
| MemoryExtensionSortIComparer | 100000  |   1,120.306 us |         - |
| TimSortIComparer             | 100000  |   2,684.499 us |    1752 B |
| BinarySortIComparer          | 100000  |   6,270.922 us |         - |
| SystemArraySort              | 1000000 |  13,118.688 us |         - |
| SystemArraySortIComparer     | 1000000 |  11,452.323 us |         - |
| SystemArraySortDelegate      | 1000000 |  14,770.843 us |         - |
| MemoryExtensionSortIComparer | 1000000 |  13,240.150 us |         - |
| TimSortIComparer             | 1000000 |  26,250.633 us |    1752 B |
| BinarySortIComparer          | 1000000 | 231,492.564 us |         - |

### Note on IComparer performance
As we all aware calling interface method is very expensive in such very tight loops. But somehow .net Comparer&lt;int&gt;.Default overcome that. Here is partially sorted benchmark with manually written comparer which was unable to devirtuallize interface call:

| Method                       | N       | Mean           | Allocated |
|----------------------------- |-------- |---------------:|----------:|
| SystemArraySort              | 1000    |       5.300 us |         - |
| SystemArraySortIComparer     | 1000    |      21.364 us |      64 B |
| MemoryExtensionSortIComparer | 1000    |      19.919 us |      64 B |
| TimSortIComparer             | 1000    |      32.038 us |    1752 B |
| BinarySortIComparer          | 1000    |      40.932 us |         - |
| SystemArraySort              | 10000   |      81.792 us |         - |
| SystemArraySortIComparer     | 10000   |     321.851 us |      64 B |
| MemoryExtensionSortIComparer | 10000   |     288.538 us |      64 B |
| TimSortIComparer             | 10000   |     393.447 us |    1752 B |
| BinarySortIComparer          | 10000   |     399.864 us |         - |
| SystemArraySort              | 100000  |   1,083.958 us |         - |
| SystemArraySortIComparer     | 100000  |   3,596.538 us |      64 B |
| MemoryExtensionSortIComparer | 100000  |   3,591.223 us |      64 B |
| TimSortIComparer             | 100000  |   4,442.471 us |    1752 B |
| BinarySortIComparer          | 100000  |   5,904.150 us |         - |
| SystemArraySort              | 1000000 |  13,334.067 us |         - |
| SystemArraySortIComparer     | 1000000 |  14,546.300 us |      64 B |
| MemoryExtensionSortIComparer | 1000000 |  14,648.573 us |      64 B |
| TimSortIComparer             | 1000000 |  21,778.414 us |    1752 B |
| BinarySortIComparer          | 1000000 | 218,587.614 us |         - |