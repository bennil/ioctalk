```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22621.2861/22H2/2022Update/SunValley2)
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2


```
| Method                                    | Categories            | Mean | Error | StdErr | StdDev | Min | Q1 | Median | Q3 | Max | Op/s | Ratio | RatioSD | Alloc Ratio |
|------------------------------------------ |---------------------- |-----:|------:|-------:|-------:|----:|---:|-------:|---:|----:|-----:|------:|--------:|------------:|
| ComplexRoundtripAsyncJson                 | ComplexAsyncRoundtrip |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
| ComplexRoundtripAsyncBinary               | ComplexAsyncRoundtrip |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
|                                           |                       |      |       |        |        |     |    |        |    |     |      |       |         |             |
| ComplexCallClientToServiceJson            | ComplexCall           |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
| ComplexCallClientToServiceBinary          | ComplexCall           |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
|                                           |                       |      |       |        |        |     |    |        |    |     |      |       |         |             |
| SimpleCallClientToServiceJson             | SimpleCall            |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
| SimpleCallClientToServiceBinary           | SimpleCall            |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
|                                           |                       |      |       |        |        |     |    |        |    |     |      |       |         |             |
| SimpleCallAsyncAwaitClientToServiceJson   | SimpleCallAsyncAwait  |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |
| SimpleCallAsyncAwaitClientToServiceBinary | SimpleCallAsyncAwait  |   NA |    NA |     NA |     NA |  NA | NA |     NA | NA |  NA |   NA |     ? |       ? |           ? |

Benchmarks with issues:
  RemoteCallsBenchmarkNetTcp.ComplexRoundtripAsyncJson: DefaultJob
  RemoteCallsBenchmarkNetTcp.ComplexRoundtripAsyncBinary: DefaultJob
  RemoteCallsBenchmarkNetTcp.ComplexCallClientToServiceJson: DefaultJob
  RemoteCallsBenchmarkNetTcp.ComplexCallClientToServiceBinary: DefaultJob
  RemoteCallsBenchmarkNetTcp.SimpleCallClientToServiceJson: DefaultJob
  RemoteCallsBenchmarkNetTcp.SimpleCallClientToServiceBinary: DefaultJob
  RemoteCallsBenchmarkNetTcp.SimpleCallAsyncAwaitClientToServiceJson: DefaultJob
  RemoteCallsBenchmarkNetTcp.SimpleCallAsyncAwaitClientToServiceBinary: DefaultJob
