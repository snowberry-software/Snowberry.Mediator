using BenchmarkDotNet.Running;
using Snowberry.Mediator.Benchmarks;

BenchmarkRunner.Run<MediatorBenchmarks>(args: args);
