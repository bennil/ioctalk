using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;

namespace IOCTalk.BenchmarkDotNet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start ioctalk benchmarks...");


            // Debug only
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());

            var summary = BenchmarkRunner.Run<RemoteCallsBenchmark>();
        }
    }
}
