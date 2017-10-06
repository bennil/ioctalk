using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Xml.Linq;
using BSAG.IOCTalk.Common.Interface.Session;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;

namespace BSAG.IOCTalk.Logging.DataStream
{
    /// <summary>
    /// Logs the data stream
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 9/9/2014 12:10:58 PM.
    ///  </para>
    /// </remarks>
    public class DataStreamLogger : IDataStreamLogger
    {
        #region fields

        private string name;
        private bool isProcessingStoreLogData = false;
        private ConcurrentQueue<StreamLogItem> dataStreamQueue = null;
        private int keepStreamLogsDays = 10;
        private TimeSpan storeWaitInterval = new TimeSpan(0, 0, 10);

        private Thread storeLogItemsThread = null;
        private ManualResetEvent stopSignal;
        private string targetFilePath;
        private ILogger log;
        private string targetDir = @"." + Path.DirectorySeparatorChar + "IOCTalk-DataStreamLogs";

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>DataStreamLogger</c>.
        /// </summary>
        public DataStreamLogger()
        {
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the target dir.
        /// </summary>
        /// <value>
        /// The target dir.
        /// </value>
        public string TargetDir
        {
            get { return targetDir; }
            set { targetDir = value; }
        }

        /// <summary>
        /// Gets or sets the old files days period
        /// </summary>
        public int KeepStreamLogsDays
        {
            get { return keepStreamLogsDays; }
            set { keepStreamLogsDays = value; }
        }


        #endregion

        #region methods


        /// <summary>
        /// Inits the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="loggerName">Name of the logger.</param>
        /// <param name="configXml">The config XML.</param>
        public void Init(IGenericCommunicationService source, string loggerName, XElement configXml)
        {
            this.log = source.Logger;
            this.name = loggerName;
            
            if (dataStreamQueue == null)
            {
                dataStreamQueue = new ConcurrentQueue<StreamLogItem>();
            }
            else
            {
                throw new InvalidOperationException("Logger \"" + name + "\" already initialized!");
            }

            // load config
            XElement targetDirElement;
            if (configXml != null
                && (targetDirElement = configXml.Element("TargetDir")) != null)
            {
                this.TargetDir = targetDirElement.Value;
            }
            else
            {
                if (!string.IsNullOrEmpty(this.TargetDir))
                {
                    this.TargetDir = Path.GetFullPath(this.TargetDir);
                }
                else
                {
                    this.TargetDir = Environment.CurrentDirectory;
                }
            }

            XElement keepStreamLogsDaysElement;
            if (configXml != null
                && (keepStreamLogsDaysElement = configXml.Element("KeepStreamLogsDays")) != null)
            {
                keepStreamLogsDays = int.Parse(keepStreamLogsDaysElement.Value);
            }


            // start logging
            stopSignal = new ManualResetEvent(false);
            isProcessingStoreLogData = true;

            storeLogItemsThread = new Thread(new ThreadStart(StoreLogItemsThread));
            storeLogItemsThread.Priority = ThreadPriority.BelowNormal;
            storeLogItemsThread.Start();
        }


        /// <summary>
        /// Stops the asynchronous logging.
        /// </summary>
        public void Dispose()
        {
            isProcessingStoreLogData = false;
            if (stopSignal != null)
            {
                stopSignal.Set();
            }
        }

        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageData">The message data.</param>
        public void LogStreamMessage(int sessionId, bool isReceive, byte[] messageData)
        {
            dataStreamQueue.Enqueue(new StreamLogItem(sessionId, isReceive, messageData));
        }

        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageDataString">The message data string.</param>
        public void LogStreamMessage(int sessionId, bool isReceive, string messageDataString)
        {
            dataStreamQueue.Enqueue(new StreamLogItem(sessionId, isReceive, messageDataString));
        }

        /// <summary>
        /// Called when [session created].
        /// </summary>
        /// <param name="session"></param>
        public void OnSessionCreated(ISession session)
        {
            string sessionInfo = string.Format("Session Created - ID: {0}; Description: {1}", session.SessionId, session.Description);

            dataStreamQueue.Enqueue(new StreamLogItem(session.SessionId, true, sessionInfo));
        }

        /// <summary>
        /// Called when the session is terminated.
        /// </summary>
        /// <param name="session"></param>
        public void OnSessionTerminated(ISession session)
        {
            string sessionInfo = string.Format("Session Terminated - ID: {0}", session.SessionId);

            dataStreamQueue.Enqueue(new StreamLogItem(session.SessionId, true, sessionInfo));
        }


        private void StoreLogItemsThread()
        {
            try
            {
                if (isProcessingStoreLogData)
                {
                    string targetDir = Path.GetFullPath(TargetDir);

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    DeleteOldLogFiles(targetDir);

                    targetFilePath = Path.Combine(targetDir, string.Concat("DataStream_", name, "_", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff"), ".dlog"));

                    log.Info(string.Format("Log data stream in \"{0}\"", targetFilePath));

                    using (StreamWriter sw = new StreamWriter(targetFilePath))
                    {
                        bool lastFlush = false;
                        while (isProcessingStoreLogData)
                        {
                            StreamLogItem item;
                            if (dataStreamQueue.TryDequeue(out item))
                            {
                                string logLine = item.CreateLogString();
                                sw.WriteLine(logLine);

                                lastFlush = false;
                            }
                            else
                            {
                                if (!lastFlush)
                                {
                                    sw.Flush();
                                    lastFlush = true;
                                }

                                if (stopSignal.WaitOne(storeWaitInterval))
                                {
                                    // stop signal received
                                    var stopLogger = new StreamLogItem(0, true, "[Logger Stopped]");
                                    sw.WriteLine(stopLogger.CreateLogString());
                                    sw.Flush();

                                    log.Info("Data stream logging stopped");

                                    isProcessingStoreLogData = false;
                                    break;
                                }
                            }
                        }
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            finally
            {
                isProcessingStoreLogData = false;
            }
        }


        private void DeleteOldLogFiles(string directory)
        {
            try
            {
                DateTime deleteTimeLimit = DateTime.Now.AddDays(-keepStreamLogsDays);

                string[] files = Directory.GetFiles(directory, "*.dlog");

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];

                    DateTime lastWriteTime = File.GetLastWriteTime(file);

                    if (lastWriteTime < deleteTimeLimit)
                    {
                        File.Delete(file);

                        log.Info(string.Format("Old data stream file \"{0}\" from {1} deleted", file, lastWriteTime));
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }


        #endregion

    }
}
