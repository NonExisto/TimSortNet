# TimSortNet
A direct translation of python list sort algorithm (aka TimSort) written in C into modern C# using generics and Span&lt;T> to fully leverage its technical edge in terms of memory access pattern while providing fully safe implementation and a little configuration support too

## Scope
Whole idea of this repo is to create copy of existing C code and keep it as close at possible to original here:
http://svn.python.org/projects/python/trunk/Objects/listobject.c

Most relevant code comments are copied as is. They are referencing documentation at http://svn.python.org/projects/python/trunk/Objects/listsort.txt

## Usage
This project is code only, i.e. no nuget package will be available. To use it just clone it to your local machine and reference existing project from you current working solution. I really like Rust development approach on code reuse

## Benchmark

| Method                   | N     | Mean           | Error       | StdDev      | Median         |
|------------------------- |------ |---------------:|------------:|------------:|---------------:|
| SystemArraySort          | 10    |       835.1 ns |    196.1 ns |    568.8 ns |       700.0 ns |
| SystemArraySortIComparer | 10    |       684.4 ns |    249.2 ns |    719.0 ns |       300.0 ns |
| TimSortIComparer         | 10    |     2,522.8 ns |    300.7 ns |    848.0 ns |     2,300.0 ns |
| BinarySortIComparer      | 10    |     1,588.0 ns |    125.5 ns |    353.9 ns |     1,500.0 ns |
| SystemArraySort          | 1000  |    21,468.4 ns |    429.3 ns |    933.3 ns |    21,400.0 ns |
| SystemArraySortIComparer | 1000  |    20,260.6 ns |    407.4 ns |  1,101.3 ns |    19,950.0 ns |
| TimSortIComparer         | 1000  |             NA |          NA |          NA |             NA |
| BinarySortIComparer      | 1000  |   105,213.3 ns |  1,010.9 ns |    945.6 ns |   104,900.0 ns |
| SystemArraySort          | 10000 |   394,371.4 ns |  2,263.1 ns |  2,006.2 ns |   394,100.0 ns |
| SystemArraySortIComparer | 10000 |   395,130.8 ns |  3,450.9 ns |  2,881.7 ns |   394,600.0 ns |
| TimSortIComparer         | 10000 |             NA |          NA |          NA |             NA |
| BinarySortIComparer      | 10000 | 2,453,057.1 ns | 15,464.6 ns | 13,709.0 ns | 2,448,150.0 ns |

Apparently there are few bugs in implementation