using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Logging;
using BSAG.IOCTalk.Common.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Logging.DataStream.Replay
{
    /// <summary>
    /// Provides helper methods to control a interface call replay from a previous recorded ioctalk data stream.
    /// This can be used to fallback to the latest received data for fast startup or fallback scenarios if the enpoint is not reachable anymore.
    /// The determination which locally recorded incoming call should be executed is directed to an external functional filter.
    /// </summary>
    public class ReplayDataStreamProvider
    {
        public const char CharQuotationMark = '\"';
        public const string Comma = ",";

        private const string ReceiveTag = "R";
        private const string SendTag = "S";

        private const string PayloadJsonKey = "Payload";

        private ILogger log;
        private DataStreamLogger streamLogger;

        public ReplayDataStreamProvider()
        {
        }


        public Func<string, string> ModifyRawJsonHandler { get; set; }


        public void Init(ILogger log, DataStreamLogger streamLogger)
        {
            this.log = log;
            this.streamLogger = streamLogger;
        }

        /// <summary>
        /// Gets the previous recorded data stream logger files
        /// </summary>
        /// <returns></returns>
        public string[] GetDataStreamLoggerFiles()
        {
            if (Directory.Exists(streamLogger.TargetDir))
            {
                string[] files = Directory.GetFiles(streamLogger.TargetDir, $"{DataStreamLogger.DataStreamFilenamePrefix}{streamLogger.Name}*.dlog");

                List<string> resultList = new List<string>(files.Length);

                DateTime currentDate = DateTime.Now.Date;
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];

                    if (file == streamLogger.CurrentTargetFilePath)
                        continue;

                    resultList.Add(file);
                }

                return resultList.OrderByDescending(name => name).ToArray();
            }
            else
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Determines the latest recorded stream file
        /// </summary>
        /// <param name="dataStreamFilePath"></param>
        /// <param name="ignoreCurrentDay">If <c>true</c> only stream files from yesterday or earlier will be examined; <c>false</c> otherwise</param>
        /// <returns></returns>
        public bool TryGetLatestStreamFile(out string dataStreamFilePath, bool ignoreCurrentDay = false)
        {
            var files = GetDataStreamLoggerFiles();

            DateTime? latestTimeUtc = null;
            string latestPath = null;
            DateTime currentDateUtc = DateTime.Now.Date.ToUniversalTime();
            foreach (var file in files)
            {
                DateTime createTimeUtc = File.GetCreationTimeUtc(file);

                if (!latestTimeUtc.HasValue || latestTimeUtc.Value < createTimeUtc)
                {
                    if (ignoreCurrentDay == false || createTimeUtc < currentDateUtc)
                    {
                        latestPath = file;
                        latestTimeUtc = createTimeUtc;
                    }
                }

            }

            if (latestTimeUtc.HasValue)
            {
                dataStreamFilePath = latestPath;
                return true;
            }
            else
            {
                dataStreamFilePath = null;
                return false;
            }
        }

        public IEnumerable<InvokeRequest> IterateDataStreamCalls(string dataStreamFile)
        {
            using (StreamReader reader = new StreamReader(dataStreamFile))
            {
                Dictionary<int, ReplaySession> sessions = new Dictionary<int, ReplaySession>();
                ReplaySession currentSession = null;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var invokeRequest = LoadStreamRow(sessions, ref currentSession, line);

                    if (invokeRequest != null)
                        yield return invokeRequest;
                }
            }
        }

        public InvokeRequest LoadStreamRow(Dictionary<int, ReplaySession> sessions, ref ReplaySession currentSession, string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string[] parts = line.Split('\t');

                int remoteHostId;
                if (int.TryParse(parts[1], out remoteHostId))
                {
                    string timeStr = parts[0];
                    TimeSpan time = TimeSpan.Parse(timeStr);
                    string direction = parts[2];
                    string message = parts[3];

                    if (message.StartsWith(DataStreamLogger.SessionCreatedTag))
                    {
                        ReplaySession session = new ReplaySession(streamLogger.CommunicationSource, remoteHostId, $"Replay session {remoteHostId} - {time}");
                        sessions.Add(session.SessionId, session);
                        currentSession = session;
                    }
                    else if (message.StartsWith(DataStreamLogger.SessionTerminatedTag))
                    {
                    }
                    else if (!message.StartsWith(DataStreamLogger.LoggerStoppedTag))
                    {
                        if (currentSession == null)
                        {
                            if (!sessions.TryGetValue(remoteHostId, out currentSession))
                            {
                                log.Error("ReplayError: Could not find session row in source stream file for session ID " + remoteHostId);
                            }
                        }
                        else if (currentSession.SessionId != remoteHostId)
                        {
                            currentSession = sessions[remoteHostId];
                        }

                        MessageType msgType = (MessageType)short.Parse(GetJsonSimpleStringValue(message, nameof(IGenericMessage.Type)));
                        long requestId = long.Parse(GetJsonSimpleStringValue(message, nameof(IGenericMessage.RequestId)));
                        string targetInterface = GetJsonSimpleStringValue(message, nameof(IGenericMessage.Target));
                        string methodName = GetJsonSimpleStringValue(message, nameof(IGenericMessage.Name));
                        //string payload = GetPayloadFromMessageString(message);

                        bool isReceive = direction == ReceiveTag;

                        if (isReceive)
                        {
                            if (msgType == MessageType.MethodInvokeRequest
                                || msgType == MessageType.AsyncMethodInvokeRequest)
                            {
                                InvokeRequest invokeReq = new InvokeRequest
                                {
                                    TargetInterfaceName = targetInterface,
                                    TargetMethodName = methodName,
                                    //Payload = payload,
                                    RequestId = requestId,
                                    Session = currentSession,
                                    MessageData = message
                                };
                                return invokeReq;
                            }
                        }
                        // else: do not replay own sent calls
                    }
                }
                else
                {
                    log.Error($"ReplayError: invalid row error! Line: \"{line}\"");
                }
            }
            return null;
        }

        private string GetPayloadFromMessageString(string message)
        {
            string payload = GetJsonSimpleStringValue(message, PayloadJsonKey);

            if (ModifyRawJsonHandler != null)
            {
                payload = ModifyRawJsonHandler(payload);
            }

            return payload;
        }


        private static string GetJsonSimpleStringValue(string json, string key)
        {
            string keyTag = "\"" + key + "\":";
            int startIndex = json.IndexOf(keyTag);
            int endIndex;

            startIndex += keyTag.Length;

            if (json[startIndex] == 'n'
                  && json[startIndex + 1] == 'u'
                  && json[startIndex + 2] == 'l'
                  && json[startIndex + 3] == 'l')
            {
                return null;
            }

            if (json[startIndex] == CharQuotationMark)
            {
                startIndex++;

                // value ends with quotation mark
                endIndex = json.IndexOf(CharQuotationMark, startIndex);
            }
            else
            {
                // value ends with comma
                endIndex = json.IndexOf(Comma, startIndex);

                if (endIndex == -1)
                {
                    endIndex = json.IndexOf('}', startIndex);
                }
            }

            if (key == PayloadJsonKey)
            {
                // read to the end - 1
                return json.Substring(startIndex, json.Length - startIndex - 1);
            }
            else
            {
                return json.Substring(startIndex, endIndex - startIndex);
            }
        }
    }
}
