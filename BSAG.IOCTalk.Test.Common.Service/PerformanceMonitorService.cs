using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Timers;
using BSAG.IOCTalk.Test.Common.Exceptions;

namespace BSAG.IOCTalk.Test.Common.Service.MEF
{
    [Export(typeof(IPerformanceMonitorService))]
    public class PerformanceMonitorService : IPerformanceMonitorService
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramCounter;
        private Timer timer;

        public PerformanceMonitorService()
        {
            cpuCounter = new PerformanceCounter();

            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        /// <summary>
        /// Gets or sets the performance monitor client notification.
        /// </summary>
        /// <value>
        /// The performance monitor client notification.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IPerformanceMonitorClientNotification PerformanceMonitorClientNotification { get; set; }


        IPerfSubscribeResponse IPerformanceMonitorService.SubscribeCpuUsageNotification(TimeSpan interval, out string test, IEnumerable<string> testCollection)
        {
            //throw new InvalidOperationException("Test exception!!");
            //throw new CustomTestException("Test message", 433452, "hello world!");


            test = "Hallo";

            if (timer != null)
                return new PerfSubscribeResponse() { SubscsrbeId = 0, Time = DateTime.Now }; // already subscribed

            // provoke lock
            PerformanceMonitorClientNotification.OnPerformancedDataSubscribed();

            this.timer = new Timer(interval.TotalMilliseconds);
            this.timer.Elapsed += new ElapsedEventHandler(OnTimer_Elapsed);
            this.timer.Start();

            return new PerfSubscribeResponse() { SubscsrbeId = 1, Time = DateTime.Now }; ;
        }




        IPerfSubscribeResponse IPerformanceMonitorService.SubscribeCpuUsageNotification(TimeSpan interval, IEnumerable<string> testCollection)
        {
            if (timer != null)
                return new PerfSubscribeResponse() { SubscsrbeId = 0, Time = DateTime.Now }; // already subscribed

            this.timer = new Timer(interval.TotalMilliseconds);
            this.timer.Elapsed += new ElapsedEventHandler(OnTimer_Elapsed);
            this.timer.Start();

            return new PerfSubscribeResponse() { SubscsrbeId = 2, Time = DateTime.Now }; ;
        }



        IPerfSubscribeResponse IPerformanceMonitorService.TestWithoutParameters()
        {
            return new PerfSubscribeResponse() { SubscsrbeId = 3, Time = DateTime.Now }; ;
        }

        private void OnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PerformanceData perfData = new PerformanceData()
            {
                Type = MeasureType.Cpu,
                Value = (decimal)cpuCounter.NextValue(),
                Unity = "%"
            };


            PerformanceMonitorClientNotification.OnPerformanceData(perfData, Environment.MachineName);
        }

        public void UnsubscribeCpuUsageNotification()
        {
            if (timer != null)
            {
                timer.Stop();
                timer = null;
            }
        }






        public MeasureType TestWithEnumReturn()
        {
            PerformanceMonitorClientNotification.NestedTestCall();
            return MeasureType.Cpu;
        }

        public MeasureType TestWithEnum(string test)
        {
            return MeasureType.Cpu;
        }

        public MeasureType TestWithEnum(string test, MeasureType measureType)
        {
            return measureType;
        }


        public IList<string> GetItems()
        {
            List<string> items = new List<string>();
            items.Add("Item1");
            items.Add("Item2");
            items.Add("Item3");
            return items;
        }
    }
}
