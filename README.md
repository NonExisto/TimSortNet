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

| Method                   | N       | Mean                | Allocated |
|------------------------- |-------- |--------------------:|----------:|
| SystemArraySort          | 10      |            572.8 ns |         - |
| SystemArraySortIComparer | 10      |            992.4 ns |         - |
| TimSortIComparer         | 10      |          2,496.7 ns |    1752 B |
| BinarySortIComparer      | 10      |          1,441.1 ns |         - |
| SystemArraySort          | 1000    |         19,943.8 ns |         - |
| SystemArraySortIComparer | 1000    |         21,618.6 ns |         - |
| TimSortIComparer         | 1000    |        269,348.1 ns |    1752 B |
| BinarySortIComparer      | 1000    |        105,585.0 ns |         - |
| SystemArraySort          | 10000   |        390,971.4 ns |         - |
| SystemArraySortIComparer | 10000   |        395,578.6 ns |         - |
| TimSortIComparer         | 10000   |      2,065,449.5 ns |    1752 B |
| BinarySortIComparer      | 10000   |      2,840,365.7 ns |         - |
| SystemArraySort          | 100000  |      5,124,116.7 ns |         - |
| SystemArraySortIComparer | 100000  |      4,985,600.0 ns |         - |
| TimSortIComparer         | 100000  |     20,250,959.4 ns |    1752 B |
| BinarySortIComparer      | 100000  |    226,261,342.9 ns |         - |
| SystemArraySort          | 1000000 |     57,480,126.7 ns |         - |
| SystemArraySortIComparer | 1000000 |     57,213,746.7 ns |         - |
| TimSortIComparer         | 1000000 |    299,749,442.9 ns |    1752 B |
| BinarySortIComparer      | 1000000 | 21,681,304,138.5 ns |         - |

and for a partially sorted data

| Method                   | N       | Mean           | Allocated |
|------------------------- |-------- |---------------:|----------:|
| SystemArraySort          | 1000    |       7.677 us |         - |
| SystemArraySortIComparer | 1000    |       9.300 us |         - |
| TimSortIComparer         | 1000    |      28.997 us |    1752 B |
| BinarySortIComparer      | 1000    |      43.496 us |         - |
| SystemArraySort          | 10000   |      81.515 us |         - |
| SystemArraySortIComparer | 10000   |      82.246 us |         - |
| TimSortIComparer         | 10000   |     374.659 us |    1752 B |
| BinarySortIComparer      | 10000   |     417.871 us |         - |
| SystemArraySort          | 100000  |   1,087.887 us |         - |
| SystemArraySortIComparer | 100000  |   1,092.129 us |         - |
| TimSortIComparer         | 100000  |   4,251.521 us |    1752 B |
| BinarySortIComparer      | 100000  |   6,203.113 us |         - |
| SystemArraySort          | 1000000 |  11,408.644 us |         - |
| SystemArraySortIComparer | 1000000 |  11,253.833 us |         - |
| TimSortIComparer         | 1000000 |  23,031.913 us |    1752 B |
| BinarySortIComparer      | 1000000 | 227,929.431 us |         - |