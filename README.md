# TimSortNet
A direct translation of python list sort algorithm (aka TimSort) written in C into modern C# using generics and Span&lt;T> to fully leverage its technical edge in terms of memory access pattern while providing fully safe implementation and a little configuration support too

## Scope
Whole idea of this repo is to create copy of existing C code and keep it as close at possible to original here:
http://svn.python.org/projects/python/trunk/Objects/listobject.c

Most relevant code comments are copied as is. They are referencing documentation at http://svn.python.org/projects/python/trunk/Objects/listsort.txt

## Usage
This project is code only, i.e. no nuget package will be available. To use it just clone it to your local machine and reference existing project from you current working solution. I really like Rust development approach on code reuse

## Benchmark
TODO

