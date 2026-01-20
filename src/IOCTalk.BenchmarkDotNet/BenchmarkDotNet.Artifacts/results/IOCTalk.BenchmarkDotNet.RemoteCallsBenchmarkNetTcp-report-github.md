```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.3194/24H2/2024Update/HudsonValley)
13th Gen Intel Core i7-13700H 2.40GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.22 (8.0.22, 8.0.2225.52707), X64 RyuJIT x86-64-v3


```
| Method                                    | Categories            | Mean     | Error    | StdDev   | StdErr   | Min      | Q1       | Median   | Q3       | Max      | Op/s     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------ |---------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ComplexRoundtripAsyncJson                 | ComplexAsyncRoundtrip | 82.35 μs | 1.634 μs | 2.182 μs | 0.436 μs | 77.17 μs | 82.76 μs | 83.17 μs | 83.54 μs | 84.53 μs | 12,143.4 |  1.00 |    0.04 | 0.4883 |   6.62 KB |        1.00 |
| ComplexRoundtripAsyncBinary               | ComplexAsyncRoundtrip | 62.77 μs | 1.039 μs | 0.972 μs | 0.251 μs | 60.39 μs | 62.30 μs | 62.85 μs | 63.32 μs | 64.29 μs | 15,932.0 |  0.76 |    0.02 | 0.3662 |   4.78 KB |        0.72 |
|                                           |                       |          |          |          |          |          |          |          |          |          |          |       |         |        |           |             |
| ComplexCallClientToServiceJson            | ComplexCall           | 67.82 μs | 0.704 μs | 0.658 μs | 0.170 μs | 66.68 μs | 67.43 μs | 67.74 μs | 68.37 μs | 68.76 μs | 14,745.2 |  1.00 |    0.01 | 0.3662 |   5.27 KB |        1.00 |
| ComplexCallClientToServiceBinary          | ComplexCall           | 50.35 μs | 0.838 μs | 0.700 μs | 0.194 μs | 48.65 μs | 50.22 μs | 50.55 μs | 50.91 μs | 51.07 μs | 19,862.3 |  0.74 |    0.01 | 0.2441 |   3.57 KB |        0.68 |
|                                           |                       |          |          |          |          |          |          |          |          |          |          |       |         |        |           |             |
| SimpleCallClientToServiceJson             | SimpleCall            | 65.81 μs | 0.598 μs | 0.559 μs | 0.144 μs | 64.97 μs | 65.52 μs | 65.86 μs | 66.14 μs | 66.93 μs | 15,195.8 |  1.00 |    0.01 | 0.2441 |   3.71 KB |        1.00 |
| SimpleCallClientToServiceBinary           | SimpleCall            | 51.32 μs | 0.995 μs | 1.362 μs | 0.267 μs | 48.49 μs | 50.45 μs | 51.23 μs | 52.42 μs | 53.87 μs | 19,486.0 |  0.78 |    0.02 | 0.1221 |   2.39 KB |        0.64 |
|                                           |                       |          |          |          |          |          |          |          |          |          |          |       |         |        |           |             |
| SimpleCallAsyncAwaitClientToServiceJson   | SimpleCallAsyncAwait  | 80.25 μs | 0.632 μs | 0.591 μs | 0.153 μs | 78.85 μs | 79.93 μs | 80.37 μs | 80.71 μs | 81.04 μs | 12,461.3 |  1.00 |    0.01 | 0.3662 |   5.23 KB |        1.00 |
| SimpleCallAsyncAwaitClientToServiceBinary | SimpleCallAsyncAwait  | 61.92 μs | 1.118 μs | 0.991 μs | 0.265 μs | 59.63 μs | 61.32 μs | 62.10 μs | 62.49 μs | 63.67 μs | 16,150.9 |  0.77 |    0.01 | 0.2441 |   3.53 KB |        0.67 |
