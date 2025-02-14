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
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using System.Threading.Channels;
using System.Threading.Tasks;

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

        public const string DataStreamFilenamePrefix = "DataStream_";

        public const string LoggerStoppedTag = "[Logger Stopped]";
        public const string SessionCreatedTag = "Session Created";
        public const string SessionTerminatedTag = "Session Terminated";

        private string name;
        private Channel<StreamLogItem> dataStreamQueue = null;
        private ChannelWriter<StreamLogItem> queueWriter;
        private int keepStreamLogsDays = 10;
        private TimeSpan storeWaitInterval = new TimeSpan(0, 0, 5);

        private string targetFilePath;
        private ILogger log;
        private string targetDir = @"." + Path.DirectorySeparatorChar + "IOCTalk-DataStreamLogs";
        private RawMessageFormat messageFormat;
        private IGenericCommunicationService source;

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

        /// <summary>
        /// Gets the assigned logger name
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets the current target file path or null
        /// </summary>
        public string CurrentTargetFilePath
        {
            get { return targetFilePath; }
        }

        /// <summary>
        /// Gets the assigned communication service source
        /// </summary>
        public IGenericCommunicationService CommunicationSource
        {
            get { return source; }
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
            this.source = source;
            this.log = source.Logger;
            this.name = loggerName;
            this.messageFormat = source.Serializer.MessageFormat;

            if (dataStreamQueue == null)
            {
                UnboundedChannelOptions channelOptions = new UnboundedChannelOptions();
                channelOptions.SingleReader = true;
                channelOptions.SingleWriter = false;

                dataStreamQueue = Channel.CreateUnbounded<StreamLogItem>(channelOptions);
                queueWriter = dataStreamQueue.Writer;
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
            Task.Run(StoreLogItemsThread);
        }


        /// <summary>
        /// Stops the asynchronous logging.
        /// </summary>
        public void Dispose()
        {
            if (dataStreamQueue != null)
            {
                dataStreamQueue.Writer.TryComplete();   // release caller queue
            }
        }

        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageData">The message data.</param>
        public void LogStreamMessage(int sessionId, bool isReceive, byte[] messageData, bool encodeBase64)
        {
            if (!queueWriter.TryWrite(new StreamLogItem(sessionId, isReceive, messageData, encodeBase64)))
                log.Warn("Could not write data stream item to queue!");
        }


        public void LogStreamMessage(int sessionId, bool isReceive, byte[] messageData, int messageLength, bool encodeBase64)
        {
            if (!queueWriter.TryWrite(new StreamLogItem(sessionId, isReceive, messageData, messageLength, encodeBase64)))
                log.Warn("Could not write data stream item to queue!");
        }


        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageDataSegement">The message data segement.</param>
        public void LogStreamMessage(int sessionId, bool isReceive, ArraySegment<byte> messageDataSegement, bool encodeBase64)
        {
            if (!queueWriter.TryWrite(new StreamLogItem(sessionId, isReceive, messageDataSegement, encodeBase64)))
                log.Warn("Could not write data stream item to queue!");
        }

        /// <summary>
        /// Logs the stream message.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="isReceive">if set to <c>true</c> [is receive].</param>
        /// <param name="messageDataString">The message data string.</param>
        public void LogStreamMessage(int sessionId, bool isReceive, string messageDataString)
        {
            if (!queueWriter.TryWrite(new StreamLogItem(sessionId, isReceive, messageDataString)))
                log.Warn("Could not write data stream item to queue!");
        }

        /// <summary>
        /// Called when [session created].
        /// </summary>
        /// <param name="session"></param>
        public void OnSessionCreated(ISession session)
        {
            string sessionInfo = $"{SessionCreatedTag} - ID: {session.SessionId}; Description: {session.Description}; Format: {this.messageFormat}";

            if (!queueWriter.TryWrite(new StreamLogItem(session.SessionId, true, sessionInfo)))
                log.Warn("Could not write session creation to data stream item queue! Session: " + sessionInfo);
        }

        /// <summary>
        /// Called when the session is terminated.
        /// </summary>
        /// <param name="session"></param>
        public void OnSessionTerminated(ISession session)
        {
            string sessionInfo = $"{SessionTerminatedTag} - ID: {session.SessionId}";

            if (!queueWriter.TryWrite(new StreamLogItem(session.SessionId, true, sessionInfo)))
                log.Warn("Could not write session termination to data stream item queue! Session: " + sessionInfo);
        }


        private async ValueTask StoreLogItemsThread()
        {
            try
            {
                // delay file processing
                await Task.Delay(500);

                string targetDir = Path.GetFullPath(TargetDir);

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                DeleteOldLogFiles(targetDir);

                targetFilePath = Path.Combine(targetDir, string.Concat(DataStreamFilenamePrefix, name, "_", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-ffff"), ".dlog"));

                log.Info(string.Format("Log data stream in \"{0}\"", targetFilePath));

                var reader = dataStreamQueue.Reader;

                using (StreamWriter sw = new StreamWriter(targetFilePath))
                {
                    while (await reader.WaitToReadAsync())
                    {
                        StreamLogItem item;
                        while (reader.TryRead(out item))
                        {
                            string logLine = item.CreateLogString();
                            sw.WriteLine(logLine);
                        }

                        sw.Flush();

                        // minimize execution priority
                        await Task.Delay(storeWaitInterval);
                    }

                    // stop signal received
                    var stopLogger = new StreamLogItem(0, true, LoggerStoppedTag);
                    sw.WriteLine(stopLogger.CreateLogString());
                    sw.Flush();
                    sw.Close();

                    log.Info("Data stream logging stopped");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            finally
            {
                targetFilePath = null;
            }
        }


        private void DeleteOldLogFiles(string directory)
        {
            try
            {
                DateTime deleteTimeLimit = DateTime.UtcNow.AddDays(-keepStreamLogsDays);

                string[] files = Directory.GetFiles(directory, "*.dlog");

                int deletedFileCount = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];

                    DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(file);

                    if (lastWriteTimeUtc < deleteTimeLimit)
                    {
                        log.Info($"Delete old data stream file \"{file}\" from {lastWriteTimeUtc} UTC");

                        File.Delete(file);
                        deletedFileCount++;
                    }
                }

                if (deletedFileCount > 0)
                    log.Info($"{deletedFileCount} outdated data stream file(s) deleted; KeepStreamLogsDays: {keepStreamLogsDays}; Expiry date: {deleteTimeLimit.ToShortDateString()}");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }



        #endregion

    }
}
