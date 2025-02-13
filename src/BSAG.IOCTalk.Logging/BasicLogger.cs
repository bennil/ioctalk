using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BSAG.IOCTalk.Logging
{
    /// <summary>
    /// Basic console and file logger
    /// </summary>
    public class BasicLogger : ILogger, IDisposable
    {
        private string path;
        private StreamWriter fileWriter;

        private static object syncObj = new object();

        public string LogRootPath { get; set; } = "log";

        public int KeepLogPeriodDays { get; set; } = 20;

        public int LogLevelInt { get; set; } = 0;

        Channel<string> logItemQueue;


        public BasicLogger()
            : this("MAIN")
        {
        }

        public BasicLogger(string categoryName, string logRootPath = "log", int keepLogPeriodDays = 20)
        {
            lock (syncObj)  // do not create in parallel
            {
                BoundedChannelOptions channelOptions = new BoundedChannelOptions(2048);
                channelOptions.FullMode = BoundedChannelFullMode.Wait;
                channelOptions.SingleReader = true;
                channelOptions.SingleWriter = false;

                logItemQueue = Channel.CreateBounded<string>(channelOptions);

                Task.Run(WriteLogItemsAsyncProcess);

                this.LogRootPath = logRootPath;
                this.KeepLogPeriodDays = keepLogPeriodDays;

                string dir = Path.GetFullPath(LogRootPath);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                path = Path.Combine(dir, $"Log_{categoryName}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")}.log");

                fileWriter = new StreamWriter(path);

                // delete old logs
                try
                {
                    foreach (string logPath in Directory.GetFiles(dir, "*.log"))
                    {
                        if (logPath == path)
                            continue;   // ignore current log

                        DateTime creationTime = File.GetLastWriteTime(logPath);

                        if ((DateTime.Now - creationTime).TotalDays > KeepLogPeriodDays)
                        {
                            File.Delete(logPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore delete errors
                }
            }
        }


        public void Debug(string message)
        {
            if (LogLevelInt > 0)
                return;

            OutputLogText(message, "Debug");
        }

        public void Info(string message)
        {
            if (LogLevelInt > 1)
                return;

            OutputLogText(message, "Info ");
        }

        public void Warn(string message)
        {
            if (LogLevelInt > 2)
                return;

            OutputLogText(message, "Warn ");
        }

        public void Error(string message)
        {
            OutputLogText(message, "Error");
        }


        private void OutputLogText(string message, string type)
        {
            string text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {type}\t{message}";

            logItemQueue.Writer.TryWrite(text);
        }

        async ValueTask WriteLogItemsAsyncProcess()
        {
            try
            {
                var reader = logItemQueue.Reader;

                // todo: change to awat foreach and ReadAllAsync in future .net versions
                while (await reader.WaitToReadAsync())
                {
                    if (reader.TryRead(out var text))
                    {
                        Console.WriteLine(text);
                        fileWriter.WriteLine(text);

                        if (reader.Count == 0)
                            fileWriter.Flush();
                    }
                }

            }
            catch (Exception ex)
            {
                DirectLogOutputIgnoreLogErrors("Unexpected BasicLogger error! Logging stopped! Details: " + ex.ToString());
            }
            finally
            {
                DirectLogOutputIgnoreLogErrors("BasicLogger stopped");
            }

        }

        void DirectLogOutputIgnoreLogErrors(string message)
        {
            try
            {
                Console.WriteLine(message);

                if (fileWriter != null)
                {
                    fileWriter.WriteLine(message);
                    fileWriter.Flush();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }


        public void Dispose()
        {
            if (logItemQueue != null)
                logItemQueue.Writer.TryComplete();   // release caller queue thread

            if (fileWriter != null)
                fileWriter.Close();
        }

    }
}
