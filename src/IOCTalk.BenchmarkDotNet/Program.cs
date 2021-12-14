using BenchmarkDotNet.Running;
using System;

namespace IOCTalk.BenchmarkDotNet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start ioctalk benchmarks...");


            var summary = BenchmarkRunner.Run<RemoteCalls>();
        }
    }
}
