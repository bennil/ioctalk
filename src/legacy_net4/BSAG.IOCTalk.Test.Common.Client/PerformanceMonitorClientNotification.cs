using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace BSAG.IOCTalk.Test.Common.Client.MEF
{
    [Export(typeof(IPerformanceMonitorClientNotification))]
    public class PerformanceMonitorClientNotification : IPerformanceMonitorClientNotification
    {
        /// <summary>
        /// Gets or sets the performance monitor service.
        /// </summary>
        /// <value>
        /// The performance monitor service.
        /// </value>
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IPerformanceMonitorService PerformanceMonitorService { get; set; }


        public void OnPerformanceData(IPerformanceData perfData)
        {
            Console.WriteLine(string.Format("Perf.data {0} = {1} {2}", perfData.Type, perfData.Value, perfData.Unity));            
        }


        public void OnPerformanceData(IPerformanceData perfData, string computerName)
        {
            Console.WriteLine(string.Format("Perf.data {0} = {1} {2} - Computer Name: {3}", perfData.Type, perfData.Value, perfData.Unity, computerName));
        }


        public void OnPerformancedDataSubscribed()
        {
            PerformanceMonitorService.TestWithEnumReturn();
        }
        
        public bool NestedTestCall()
        {
            return true;
        }
    }
}
