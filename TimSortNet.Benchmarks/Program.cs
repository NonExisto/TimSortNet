// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using TimSortNet.Benchmarks;

var summary = BenchmarkRunner.Run<SemiSortBenchmarks>();
