﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Test.TestObjects;
using BSAG.IOCTalk.Communication.Tcp;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Binary;
using BSAG.IOCTalk.Test.Common.Service;
using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOCTalk.BenchmarkDotNet
{
    //[RPlotExporter]
    [AllStatisticsColumn]
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    //[EtwProfiler(performExtraBenchmarksRun: false)]
    public class RemoteCallsBenchmark
    {
        TaskCompletionSource<bool> onConnectionEstablished;
        static IStressTestService currentStressTestServiceClientProxyInstanceJson;
        static IMyRemoteAsyncAwaitTestService myRemoteAsyncAwaitTestServiceJson;

        static IStressTestService currentStressTestServiceClientProxyInstanceBinary;
        static IMyRemoteAsyncAwaitTestService myRemoteAsyncAwaitTestServiceBinary;

        TcpCommunicationController tcpClientJson;
        TcpCommunicationController tcpBackendServiceJson;

        TcpCommunicationController tcpClientBinary;
        TcpCommunicationController tcpBackendServiceBinary;

        static object syncObj = new object();

        [GlobalSetup]
        //public async Task Setup()     // async bug in BenchmarkDotNet? Benchmark starts before async Setup completed!
        public void Setup()
        {
            onConnectionEstablished = new TaskCompletionSource<bool>();

            const int timeoutMs = 20000;
            var ct = new CancellationTokenSource(timeoutMs);
            ct.Token.Register(() => onConnectionEstablished.TrySetCanceled(), useSynchronizationContext: false);

            int port = 33255;


            InitClientServiceTcpWithJsonSerializer(port);

            port++;

            InitClientServiceTcpWithBinarySerializer(port);

            onConnectionEstablished.Task.Wait();
            //await onConnectionEstablished.Task;
            //await Task.Delay(200); // wait for cache sync
        }

        private void InitClientServiceTcpWithBinarySerializer(int port)
        {
            myRemoteAsyncAwaitTestServiceBinary = null;
            {
                // init service
                var compositionHostService = new TalkCompositionHost("BinaryService");
                //compositionHostService.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostService.AddAssembly(typeof(StressTestService).Assembly);

                //compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IStressTestService>();
                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService>();


                tcpBackendServiceBinary = new TcpCommunicationController();
                tcpBackendServiceBinary.Serializer = new BinaryMessageSerializer();

                compositionHostService.InitGenericCommunication(tcpBackendServiceBinary);

                tcpBackendServiceBinary.InitService(port);

                //localService = (StressTestService)compositionHostService.GetExport<IStressTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost("BinaryClient");
                //compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IStressTestService>();
                compositionHostClient.RegisterAsyncVoidMethod<IStressTestService>(nameof(IStressTestService.AsyncCallTest));

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                tcpClientBinary = new TcpCommunicationController();
                tcpClientBinary.Serializer = new BinaryMessageSerializer();

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreatedBinary;

                compositionHostClient.InitGenericCommunication(tcpClientBinary);

                tcpClientBinary.InitClient(IPAddress.Loopback.ToString(), port);
            }
        }


        private void InitClientServiceTcpWithJsonSerializer(int port)
        {
            myRemoteAsyncAwaitTestServiceJson = null;
            {
                // init service
                var compositionHostService = new TalkCompositionHost("JsonService");
                //compositionHostService.AddAssembly(typeof(IStressTestService).Assembly);
                compositionHostService.AddAssembly(typeof(StressTestService).Assembly);

                //compositionHostService.RegisterLocalSharedService<ILogger>(log);

                compositionHostService.RegisterLocalSharedService<IStressTestService>();
                compositionHostService.RegisterLocalSharedService<IMyRemoteAsyncAwaitTestService>();


                tcpBackendServiceJson = new TcpCommunicationController();

                compositionHostService.InitGenericCommunication(tcpBackendServiceJson);

                tcpBackendServiceJson.InitService(port);

                //localService = (StressTestService)compositionHostService.GetExport<IStressTestService>();
            }

            {
                // init client
                var compositionHostClient = new TalkCompositionHost("JsonClient");
                //compositionHostClient.RegisterLocalSharedService<ILogger>(log);

                compositionHostClient.RegisterRemoteService<IStressTestService>();
                compositionHostClient.RegisterAsyncVoidMethod<IStressTestService>(nameof(IStressTestService.AsyncCallTest));

                compositionHostClient.RegisterRemoteService<IMyRemoteAsyncAwaitTestService>();

                tcpClientJson = new TcpCommunicationController();

                compositionHostClient.SessionCreated += OnCompositionHostClient_SessionCreatedJson;

                compositionHostClient.InitGenericCommunication(tcpClientJson);

                tcpClientJson.InitClient(IPAddress.Loopback.ToString(), port);
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            tcpClientJson?.Shutdown();
            tcpBackendServiceJson?.Shutdown();

            tcpClientBinary?.Shutdown();
            tcpBackendServiceBinary?.Shutdown();
        }

        private void OnCompositionHostClient_SessionCreatedJson(object contractSession, SessionEventArgs e)
        {
            currentStressTestServiceClientProxyInstanceJson = e.SessionContract.GetSessionInstance<IStressTestService>();
            myRemoteAsyncAwaitTestServiceJson = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();

            CompleteOnSessionsCompleted();
        }

        private void OnCompositionHostClient_SessionCreatedBinary(object contractSession, SessionEventArgs e)
        {
            currentStressTestServiceClientProxyInstanceBinary = e.SessionContract.GetSessionInstance<IStressTestService>();
            myRemoteAsyncAwaitTestServiceBinary = e.SessionContract.GetSessionInstance<IMyRemoteAsyncAwaitTestService>();

            CompleteOnSessionsCompleted();
        }

        private void CompleteOnSessionsCompleted()
        {
            lock (syncObj)
            {
                if (myRemoteAsyncAwaitTestServiceJson != null
                    && myRemoteAsyncAwaitTestServiceBinary != null
                    && onConnectionEstablished.Task.IsCompleted == false)
                {
                    onConnectionEstablished.SetResult(true);
                }
            }
        }

        [BenchmarkCategory("SimpleCall"), Benchmark(Baseline = true)]
        public void SimpleCallClientToServiceJson() => currentStressTestServiceClientProxyInstanceJson.SimpleCall();

        [BenchmarkCategory("SimpleCall"), Benchmark]
        public void SimpleCallClientToServiceBinary() => currentStressTestServiceClientProxyInstanceBinary.SimpleCall();



        [BenchmarkCategory("SimpleCallAsyncAwait"), Benchmark(Baseline = true)]
        public async Task SimpleCallAsyncAwaitClientToServiceJson() => await myRemoteAsyncAwaitTestServiceJson.SimpleCall();

        [BenchmarkCategory("SimpleCallAsyncAwait"), Benchmark]
        public async Task SimpleCallAsyncAwaitClientToServiceBinary() => await myRemoteAsyncAwaitTestServiceBinary.SimpleCall();



        static readonly DataTransferTest complexObj = new() { ID = 1234, Name = "This is a long name" };

        [BenchmarkCategory("ComplexCall"), Benchmark(Baseline = true)]
        public void ComplexCallClientToServiceJson() => currentStressTestServiceClientProxyInstanceJson.ComplexCall(-1, complexObj);

        [BenchmarkCategory("ComplexCall"), Benchmark]
        public void ComplexCallClientToServiceBinary() => currentStressTestServiceClientProxyInstanceBinary.ComplexCall(-1, complexObj);





        [BenchmarkCategory("ComplexAsyncRoundtrip"), Benchmark(Baseline = true)]
        public async Task ComplexRoundtripAsyncJson() => await myRemoteAsyncAwaitTestServiceJson.ComplexRoundtrip(complexObj);

        [BenchmarkCategory("ComplexAsyncRoundtrip"), Benchmark]
        public async Task ComplexRoundtripAsyncBinary() => await myRemoteAsyncAwaitTestServiceBinary.ComplexRoundtrip(complexObj);
    }
}
