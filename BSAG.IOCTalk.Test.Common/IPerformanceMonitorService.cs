using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.Common
{
    public interface IPerformanceMonitorService
    {
        /// <summary>
        /// Subscribes the cpu usage notification.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <returns></returns>
        IPerfSubscribeResponse SubscribeCpuUsageNotification(TimeSpan interval, out string test, IEnumerable<string> testCollection);


        /// <summary>
        /// Subscribes the cpu usage notification.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <returns></returns>
        IPerfSubscribeResponse SubscribeCpuUsageNotification(TimeSpan interval, IEnumerable<string> testCollection);

        /// <summary>
        /// Tests without parameters.
        /// </summary>
        /// <returns></returns>
        IPerfSubscribeResponse TestWithoutParameters();

        /// <summary>
        /// Tests the with enum return.
        /// </summary>
        /// <returns></returns>
        MeasureType TestWithEnumReturn();
        
        /// <summary>
        /// Tests the with enum.
        /// </summary>
        /// <param name="test">The test.</param>
        /// <returns></returns>
        MeasureType TestWithEnum(string test);

        /// <summary>
        /// Tests the with enum.
        /// </summary>
        /// <param name="test">The test.</param>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        MeasureType TestWithEnum(string test, MeasureType measureType);


        /// <summary>
        /// Unsubscribes the cpu usage notification.
        /// </summary>
        void UnsubscribeCpuUsageNotification();

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <returns></returns>
        IList<string> GetItems();
    }
}
