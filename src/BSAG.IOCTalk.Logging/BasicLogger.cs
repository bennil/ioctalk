﻿using BSAG.IOCTalk.Common.Interface.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        public BasicLogger()
            :this("MAIN")
        {
        }

        public BasicLogger(string categoryName)
        {
            lock (syncObj)  // do not create in parallel
            {
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
            Console.WriteLine(text);
            fileWriter.WriteLine(text);
            fileWriter.Flush();
        }


        public void Dispose()
        {
            if (fileWriter != null)
                fileWriter.Close();
        }

    }
}
