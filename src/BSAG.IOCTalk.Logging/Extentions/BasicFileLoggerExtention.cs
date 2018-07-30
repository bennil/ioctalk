//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;

//namespace BSAG.IOCTalk.Logging.Extentions
//{
//    public class BasicFileLoggerExtention : ILoggerProvider, ILogger
//    {
//        private string path;
//        private StreamWriter fileWriter;

//        public BasicFileLoggerExtention()
//        {
//        }

//        public string LogRootPath { get; set; } = "log";

//        public int KeepLogPeriodDays { get; set; } = 20;

//        public ILogger CreateLogger(string categoryName)
//        {
//            if (path != null)
//                throw new InvalidOperationException("Only one logger supported");

//            string dir = Path.GetFullPath(LogRootPath);

//            if (!Directory.Exists(dir))
//                Directory.CreateDirectory(dir);

//            path = Path.Combine(dir, $"Log_{categoryName}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff")}.log");

//            fileWriter = new StreamWriter(path);

//            // delete old logs
//            try
//            {
//                foreach (string logPath in Directory.GetFiles(dir, "*.log"))
//                {
//                    if (logPath == path)
//                        continue;   // ignore current log

//                    DateTime creationTime = File.GetCreationTime(logPath);

//                    if ((DateTime.Now - creationTime).TotalDays > KeepLogPeriodDays)
//                    {
//                        File.Delete(logPath);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                // ignore delete errors
//            }

//            return this;
//        }

//        public void Dispose()
//        {
//            if (fileWriter != null)
//                fileWriter.Close();
//        }


//        public IDisposable BeginScope<TState>(TState state)
//        {
//            return this;
//        }


//        public bool IsEnabled(LogLevel logLevel)
//        {
//            return true;
//        }

//        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//        {
//            string message = formatter(state, exception);

//            fileWriter.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffff")} {logLevel} - {message}");
//            fileWriter.Flush();
//        }

//    }
//}
