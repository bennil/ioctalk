using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Test;
using BSAG.IOCTalk.Communication.NetTcp;
using BSAG.IOCTalk.Communication.NetTcp.WireFraming;
using BSAG.IOCTalk.Composition;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BSAG.IOCTalk.Serialization.Binary.Test
{
    /// <summary>
    /// Several threads send to the same session at the same time (requests on the client side, responses on the service side).
    /// The binary serializer keeps a state per session (published type meta data), therefore serialization and transfer
    /// must be one atomic step per session.
    /// </summary>
    public class ConcurrentSessionSendTest
    {
        readonly ITestOutputHelper xUnitLog;

        public ConcurrentSessionSendTest(ITestOutputHelper xUnitLog)
        {
            this.xUnitLog = xUnitLog;
        }

        [Fact]
        public async Task TestConcurrentFirstTimeSendsOnSameSession()
        {
            const int port = 33291;
            const int rounds = 15;
            const int callsPerThread = 20;

            var compositionHostService = new TalkCompositionHost("Service");
            compositionHostService.RegisterLocalSharedService<ILogger>(new UnitTestLogger(xUnitLog));
            compositionHostService.RegisterLocalSessionService<IConcurrentSendTestService, ConcurrentSendTestService>();

            var tcpService = new TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());
            tcpService.InvokeThreadModel = InvokeThreadModel.IndividualTask;   // responses of one session are sent by several threads
            compositionHostService.InitGenericCommunication(tcpService);
            tcpService.InitService(port);

            try
            {
                // every round uses a new session: the type meta data is published again by the competing threads
                for (int round = 0; round < rounds; round++)
                {
                    var serviceProxyCreated = new TaskCompletionSource<IConcurrentSendTestService>(TaskCreationOptions.RunContinuationsAsynchronously);

                    var compositionHostClient = new TalkCompositionHost("Client");
                    compositionHostClient.RegisterLocalSharedService<ILogger>(new UnitTestLogger(xUnitLog));
                    compositionHostClient.RegisterRemoteService<IConcurrentSendTestService>(true);
                    compositionHostClient.SessionCreated += (s, e) => serviceProxyCreated.TrySetResult(e.SessionContract.GetSessionInstance<IConcurrentSendTestService>());

                    var tcpClient = new TcpCommunicationController(new ShortWireFraming(), new BinaryMessageSerializer());
                    tcpClient.RequestTimeoutSeconds = 15;
                    compositionHostClient.InitGenericCommunication(tcpClient);
                    tcpClient.InitClient(IPAddress.Loopback.ToString(), port);

                    var service = await serviceProxyCreated.Task.WaitAsync(TimeSpan.FromSeconds(20));

                    Func<int, bool>[] calls = new Func<int, bool>[]
                    {
                        i => { var r = service.EchoA(new ConcurrentSendItemA { Id = i, Name = "A" + i }); return r.Id == i && r.Name == "A" + i; },
                        i => { var r = service.EchoB(new ConcurrentSendItemB { Number = i, Text = "B" + i, Time = new DateTime(2026, 1, 1).AddMinutes(i) }); return r.Number == i && r.Text == "B" + i; },
                        i => { var r = service.EchoC(new ConcurrentSendItemC { Key = i, Values = new[] { i, i + 1, i + 2 } }); return r.Key == i && r.Values.SequenceEqual(new[] { i, i + 1, i + 2 }); },
                        i => { var r = service.EchoD(new ConcurrentSendItemD { Flag = i % 2 == 0, Amount = i * 1.5m, Comment = "D" + i }); return r.Amount == i * 1.5m && r.Comment == "D" + i; },
                        i => service.EchoString("S" + i) == "S" + i,
                        i => service.EchoArray(new[] { i, -i }).SequenceEqual(new[] { i, -i }),
                    };

                    using (var startSignal = new ManualResetEventSlim(false))
                    {
                        var tasks = calls.Select(call => Task.Factory.StartNew(() =>
                        {
                            startSignal.Wait();

                            for (int i = 0; i < callsPerThread; i++)
                            {
                                Assert.True(call(i), $"Unexpected result at call {i}");
                            }
                        }, TaskCreationOptions.LongRunning)).ToArray();

                        startSignal.Set();
                        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(60));
                    }

                    tcpClient.Shutdown();
                }
            }
            finally
            {
                tcpService.Shutdown();
            }
        }
    }


    public interface IConcurrentSendTestService
    {
        ConcurrentSendItemA EchoA(ConcurrentSendItemA item);

        ConcurrentSendItemB EchoB(ConcurrentSendItemB item);

        ConcurrentSendItemC EchoC(ConcurrentSendItemC item);

        ConcurrentSendItemD EchoD(ConcurrentSendItemD item);

        string EchoString(string value);

        int[] EchoArray(int[] values);
    }

    public class ConcurrentSendTestService : IConcurrentSendTestService
    {
        public ConcurrentSendItemA EchoA(ConcurrentSendItemA item) => item;

        public ConcurrentSendItemB EchoB(ConcurrentSendItemB item) => item;

        public ConcurrentSendItemC EchoC(ConcurrentSendItemC item) => item;

        public ConcurrentSendItemD EchoD(ConcurrentSendItemD item) => item;

        public string EchoString(string value) => value;

        public int[] EchoArray(int[] values) => values;
    }

    public class ConcurrentSendItemA
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class ConcurrentSendItemB
    {
        public int Number { get; set; }

        public string Text { get; set; }

        public DateTime Time { get; set; }
    }

    public class ConcurrentSendItemC
    {
        public int Key { get; set; }

        public int[] Values { get; set; }
    }

    public class ConcurrentSendItemD
    {
        public bool Flag { get; set; }

        public decimal Amount { get; set; }

        public string Comment { get; set; }
    }
}
