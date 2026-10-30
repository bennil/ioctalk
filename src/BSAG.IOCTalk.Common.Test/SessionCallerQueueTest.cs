using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Composition;
using BSAG.IOCTalk.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Common.Test
{
    /// <summary>
    /// Tests the InvokeThreadModel.CallerThreadPerSession behaviour
    /// </summary>
    public class SessionCallerQueueTest
    {
        const int OtherSessionMaxResponseMs = 1500;

        readonly ITestOutputHelper xUnitLog;

        public SessionCallerQueueTest(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }

        [Fact]
        public async Task TestBlockingCallDoesNotDelayOtherSession()
        {
            var service = InitService(33281, InvokeThreadModel.CallerThreadPerSession);
            var clientA = await ConnectClient(33281);
            var clientB = await ConnectClient(33281);

            var blockTask = Task.Run(() => clientA.Service.Block(3000));
            await Task.Delay(300);

            var sw = Stopwatch.StartNew();
            int result = await Task.Run(() => clientB.Service.Echo(5));
            sw.Stop();

            Assert.Equal(5, result);
            Assert.True(sw.ElapsedMilliseconds < OtherSessionMaxResponseMs, $"Other session response took {sw.ElapsedMilliseconds} ms");
            Assert.Equal(3000, await blockTask);

            Shutdown(service, clientA.Tcp, clientB.Tcp);
        }

        /// <summary>
        /// Verifies the test setup: the container wide caller thread delays the other sessions
        /// </summary>
        [Fact]
        public async Task TestBlockingCallDelaysOtherSessionWithContainerCallerThread()
        {
            var service = InitService(33282, InvokeThreadModel.CallerThread);
            var clientA = await ConnectClient(33282);
            var clientB = await ConnectClient(33282);

            var blockTask = Task.Run(() => clientA.Service.Block(2000));
            await Task.Delay(300);

            var sw = Stopwatch.StartNew();
            int result = await Task.Run(() => clientB.Service.Echo(5));
            sw.Stop();

            Assert.Equal(5, result);
            Assert.True(sw.ElapsedMilliseconds >= OtherSessionMaxResponseMs, $"Other session response took only {sw.ElapsedMilliseconds} ms");
            Assert.Equal(2000, await blockTask);

            Shutdown(service, clientA.Tcp, clientB.Tcp);
        }

        [Fact]
        public async Task TestNestedBackCallOfSameSession()
        {
            var service = InitService(33283, InvokeThreadModel.CallerThreadPerSession);
            var clientA = await ConnectClient(33283);

            // service -> client.OnCallback -> service.Echo (processed during the nested response wait of the session)
            int result = await Task.Run(() => clientA.Service.CallClientBack(10));

            Assert.Equal(11, result);

            Shutdown(service, clientA.Tcp);
        }

        [Fact]
        public async Task TestInvokeOrderPerSession()
        {
            var service = InitService(33284, InvokeThreadModel.CallerThreadPerSession, sessionCallerQueueCapacity: 4);
            var clientA = await ConnectClient(33284);
            var clientB = await ConnectClient(33284);

            const int count = 500;
            var recordA = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                    clientA.Service.Record(i);
            });
            var recordB = Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                    clientB.Service.Record(-i);
            });
            await Task.WhenAll(recordA, recordB);

            int[] recordedA = await Task.Run(() => clientA.Service.GetRecorded());
            int[] recordedB = await Task.Run(() => clientB.Service.GetRecorded());

            Assert.Equal(Enumerable.Range(0, count).ToArray(), recordedA);
            Assert.Equal(Enumerable.Range(0, count).Select(i => -i).ToArray(), recordedB);

            Shutdown(service, clientA.Tcp, clientB.Tcp);
        }

        /// <summary>
        /// A session invoke waits for a non responding remote session of another container (container wide caller thread).
        /// The other sessions must not be affected.
        /// </summary>
        [Fact]
        public async Task TestCrossContainerWaitDoesNotDelayOtherSession()
        {
            // device backend container (container wide caller thread)
            var deviceProxyCreated = new TaskCompletionSource<ISessionQueueTestDevice>(TaskCreationOptions.RunContinuationsAsynchronously);
            var deviceBackendHost = new TalkCompositionHost("DeviceBackend");
            deviceBackendHost.RegisterLocalSharedService<ILogger>(new UnitTestLogger(xUnitLog));
            deviceBackendHost.RegisterRemoteService<ISessionQueueTestDevice>(true);
            deviceBackendHost.SessionCreated += (s, e) => deviceProxyCreated.TrySetResult(e.SessionContract.GetSessionInstance<ISessionQueueTestDevice>());
            var deviceBackendTcp = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
            deviceBackendHost.InitGenericCommunication(deviceBackendTcp);
            deviceBackendTcp.InitService(33286);

            // device
            var deviceHost = new TalkCompositionHost("Device");
            deviceHost.RegisterLocalSharedService<ILogger>(new UnitTestLogger(xUnitLog));
            deviceHost.RegisterLocalSessionService<ISessionQueueTestDevice, SessionQueueTestDevice>();
            var deviceTcp = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
            deviceHost.InitGenericCommunication(deviceTcp);
            deviceTcp.InitClient(IPAddress.Loopback.ToString(), 33286);

            SessionQueueTestService.DeviceProxy = await deviceProxyCreated.Task.WaitAsync(TimeSpan.FromSeconds(20));

            // frontend container
            var service = InitService(33285, InvokeThreadModel.CallerThreadPerSession);
            var clientA = await ConnectClient(33285);
            var clientB = await ConnectClient(33285);

            var forwardTask = Task.Run(() => clientA.Service.ForwardToDevice(2500));
            await Task.Delay(300);

            var sw = Stopwatch.StartNew();
            int result = await Task.Run(() => clientB.Service.Echo(7));
            sw.Stop();

            Assert.Equal(7, result);
            Assert.True(sw.ElapsedMilliseconds < OtherSessionMaxResponseMs, $"Other session response took {sw.ElapsedMilliseconds} ms");

            // nested back calls of the waiting session are still processed
            Assert.Equal(21, await Task.Run(() => clientA.Service.CallClientBack(20)));

            Assert.Equal(2500, await forwardTask);

            SessionQueueTestService.DeviceProxy = null;
            Shutdown(service, clientA.Tcp, clientB.Tcp, deviceTcp, deviceBackendTcp);
        }


        private TcpCommunicationController InitService(int port, InvokeThreadModel invokeThreadModel, int? sessionCallerQueueCapacity = null)
        {
            var compositionHostService = new TalkCompositionHost("Service");
            compositionHostService.RegisterLocalSharedService<ILogger>(new UnitTestLogger(xUnitLog));
            compositionHostService.RegisterLocalSessionService<ISessionQueueTestService, SessionQueueTestService>();
            compositionHostService.RegisterRemoteService<ISessionQueueTestClient>(true);

            var tcpService = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
            tcpService.InvokeThreadModel = invokeThreadModel;
            if (sessionCallerQueueCapacity.HasValue)
                tcpService.SessionCallerQueueCapacity = sessionCallerQueueCapacity.Value;

            compositionHostService.InitGenericCommunication(tcpService);
            tcpService.InitService(port);

            return tcpService;
        }

        private async Task<(TcpCommunicationController Tcp, ISessionQueueTestService Service)> ConnectClient(int port)
        {
            var serviceProxyCreated = new TaskCompletionSource<ISessionQueueTestService>(TaskCreationOptions.RunContinuationsAsynchronously);

            var compositionHostClient = new TalkCompositionHost("Client");
            compositionHostClient.RegisterLocalSharedService<ILogger>(new UnitTestLogger(xUnitLog));
            compositionHostClient.RegisterRemoteService<ISessionQueueTestService>(true)
                .AsAsyncVoid(nameof(ISessionQueueTestService.Record));
            compositionHostClient.RegisterLocalSessionService<ISessionQueueTestClient, SessionQueueTestClient>();
            compositionHostClient.SessionCreated += (s, e) => serviceProxyCreated.TrySetResult(e.SessionContract.GetSessionInstance<ISessionQueueTestService>());

            var tcpClient = new TcpCommunicationController(new ShortWireFraming(), new JsonMessageSerializer());
            tcpClient.RequestTimeoutSeconds = 15;

            compositionHostClient.InitGenericCommunication(tcpClient);
            tcpClient.InitClient(IPAddress.Loopback.ToString(), port);

            var serviceProxy = await serviceProxyCreated.Task.WaitAsync(TimeSpan.FromSeconds(20));

            return (tcpClient, serviceProxy);
        }

        private static void Shutdown(params TcpCommunicationController[] controllers)
        {
            foreach (var controller in controllers)
                controller.Shutdown();
        }
    }


    public interface ISessionQueueTestService
    {
        int Echo(int value);

        int Block(int milliseconds);

        int CallClientBack(int value);

        int ForwardToDevice(int milliseconds);

        void Record(int value);

        int[] GetRecorded();
    }

    public interface ISessionQueueTestClient
    {
        int OnCallback(int value);
    }

    public interface ISessionQueueTestDevice
    {
        int Hang(int milliseconds);
    }

    public class SessionQueueTestService : ISessionQueueTestService
    {
        public static ISessionQueueTestDevice DeviceProxy;

        readonly ISessionQueueTestClient client;
        readonly List<int> recorded = new List<int>();

        public SessionQueueTestService(ISessionQueueTestClient client)
        {
            this.client = client;
        }

        public int Echo(int value) => value;

        public int Block(int milliseconds)
        {
            Thread.Sleep(milliseconds);
            return milliseconds;
        }

        public int CallClientBack(int value) => client.OnCallback(value);

        public int ForwardToDevice(int milliseconds) => DeviceProxy.Hang(milliseconds);

        public void Record(int value)
        {
            lock (recorded)
                recorded.Add(value);
        }

        public int[] GetRecorded()
        {
            lock (recorded)
                return recorded.ToArray();
        }
    }

    public class SessionQueueTestClient : ISessionQueueTestClient
    {
        readonly ISessionQueueTestService service;

        public SessionQueueTestClient(ISessionQueueTestService service)
        {
            this.service = service;
        }

        public int OnCallback(int value) => service.Echo(value) + 1;
    }

    public class SessionQueueTestDevice : ISessionQueueTestDevice
    {
        public int Hang(int milliseconds)
        {
            Thread.Sleep(milliseconds);
            return milliseconds;
        }
    }
}
