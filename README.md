# TimSortNet
A direct translation of python list sort algorithm (aka TimSort) written in C into modern C# using generics and Span&lt;T> to fully leverage its technical edge in terms of memory access pattern while providing fully safe implementation and a little configuration support too

## Scope
Whole idea of this repo is to create copy of existing C code and keep it as close at possible to original here:
http://svn.python.org/projects/python/trunk/Objects/listobject.c

Most relevant code comments are copied as is. They are referencing documentation at http://svn.python.org/projects/python/trunk/Objects/listsort.txt

## Usage
This project is code only, i.e. no nuget package will be available. To use it just clone it to your local machine and reference existing project from you current working solution. I really like Rust development approach on code reuse

## Benchmark

| Method                   | N     | Mean           | Error        | StdDev      | Median         |
|------------------------- |------ |---------------:|-------------:|------------:|---------------:|
| SystemArraySort          | 10    |       409.7 ns |    120.90 ns |    343.0 ns |       300.0 ns |
| SystemArraySortIComparer | 10    |       348.3 ns |     50.71 ns |    138.8 ns |       300.0 ns |
| TimSortIComparer         | 10    |     1,862.8 ns |     97.01 ns |    263.9 ns |     1,800.0 ns |
| BinarySortIComparer      | 10    |     1,340.2 ns |    144.81 ns |    408.4 ns |     1,200.0 ns |
| SystemArraySort          | 1000  |    20,310.8 ns |    407.54 ns |  1,022.4 ns |    19,900.0 ns |
| SystemArraySortIComparer | 1000  |    20,048.2 ns |    392.99 ns |    846.0 ns |    19,750.0 ns |
| TimSortIComparer         | 1000  |   101,697.6 ns |  2,022.52 ns |  3,647.0 ns |   100,300.0 ns |
| BinarySortIComparer      | 1000  |   104,769.2 ns |  1,323.98 ns |  1,105.6 ns |   104,400.0 ns |
| SystemArraySort          | 10000 |   394,721.4 ns |  1,873.13 ns |  1,660.5 ns |   394,000.0 ns |
| SystemArraySortIComparer | 10000 |   408,266.1 ns |  8,163.07 ns | 12,465.9 ns |   406,950.0 ns |
| TimSortIComparer         | 10000 | 1,593,114.3 ns |  7,684.21 ns |  6,811.9 ns | 1,593,250.0 ns |
| BinarySortIComparer      | 10000 | 2,460,208.3 ns | 29,610.96 ns | 23,118.3 ns | 2,446,850.0 ns |

Apparently there are still few bugs in implementation