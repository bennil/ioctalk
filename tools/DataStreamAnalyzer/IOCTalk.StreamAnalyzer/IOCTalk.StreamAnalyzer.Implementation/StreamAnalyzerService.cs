using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using BSAG.IOCTalk.Communication.Common;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Serialization.Binary;
using System.Diagnostics;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;

namespace IOCTalk.StreamAnalyzer.Implementation
{
    /// <summary>
    /// Stream Analyzer service
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 11/24/2015 9:53:38 AM.
    ///  </para>
    /// </remarks>
    public class StreamAnalyzerService
    {
        #region fields

        public const char CharQuotationMark = '\"';
        public const char CharJsonArrayStart = '[';
        public const char CharJsonArrayEnd = ']';
        public const string Comma = ",";
        public const char FieldSeparator = '\t';

        private static BinaryMessageSerializer binarySerializer;

        #endregion

        #region constructors

        /// <summary>
        /// Creates and initializes an instance of the class <c>StreamAnalyzerService</c>.
        /// </summary>
        public StreamAnalyzerService()
        {
        }


        #endregion

        #region properties

        public string LastFilePath { get; set; }

        #endregion

        #region methods

        public IList<StreamSession> AnalyzeDataStreamSession(string fileName, TimeSpan? roundTripTimeFilter, int? flowRateFilter, Action<int> progressPercentage, FileExportFilter fileExort, out StringBuilder errors)
        {
            this.LastFilePath = fileName;
            errors = new StringBuilder();
            HashSet<int> ignoreSessions = new HashSet<int>();
            Dictionary<string, StreamWriter> openFileExportStreams = null;
            using (StreamReader streamReader = new StreamReader(fileName))
            {
                Dictionary<int, StreamSession> sessions = new Dictionary<int, StreamSession>();
                StreamSession currentSession = null;

                double totalFileSize = (double)streamReader.BaseStream.Length;

                string line = null;
                long lineNumber = 1;
                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] parts = line.Split(FieldSeparator);

                    if (parts.Length > 4)
                    {
                        // message contains \t in data part
                        // merge last parts
                        string lastPart = string.Join("", parts, 3, parts.Length - 3);
                        parts = new string[] { parts[0], parts[1], parts[2], lastPart };
                    }

                    if (parts.Length == 4)
                    {
                        TimeSpan time;
                        int sessionId;
                        bool isReceive;
                        if (TryParseDataStreamLine(parts, out time, out sessionId, out isReceive))
                        {
                            string dataPart = parts[3];

                            if (dataPart.StartsWith("Session Created"))
                            {
                                var sessionInfoParts = dataPart.Split(';');
                                RawMessageFormat format = RawMessageFormat.JSON;

                                if (sessionInfoParts.Length >= 3)
                                {
                                    string formatStr = sessionInfoParts[2];
                                    string[] formatParts = formatStr.Split(':');
                                    format = (RawMessageFormat)Enum.Parse(typeof(RawMessageFormat), formatParts[1].Trim());
                                }

                                currentSession = new StreamSession(sessionId, dataPart, time, format);
                                sessions.Add(sessionId, currentSession);
                            }
                            else if (dataPart.StartsWith("Session Terminated"))
                            {
                                currentSession.TerminatedAt = time;
                            }
                            else if (dataPart.StartsWith("[Dismiss invalid session message]"))
                            {
                                errors.AppendLine("Packet received without valid session!");
                                errors.AppendLine(line);
                                errors.AppendLine();
                                continue;
                            }
                            else if (!dataPart.StartsWith("[Logger Stopped]"))
                            {
                                if (currentSession == null || currentSession.SessionId != sessionId)
                                {
                                    if (!sessions.TryGetValue(sessionId, out currentSession))
                                    {
                                        if (!ignoreSessions.Contains(sessionId))
                                        {
                                            errors.Append("Session ID ");
                                            errors.Append(sessionId);
                                            errors.AppendLine(" not found! All messages from this session are ignored!");

                                            ignoreSessions.Add(sessionId);
                                        }
                                        continue;
                                    }
                                }

                                int dataLength;
                                IGenericMessage message = ParseMessage(dataPart, currentSession.Format, currentSession, out dataLength);

                                // Flow rate
                                if (currentSession.PendingFlowRate == null)
                                {
                                    currentSession.PendingFlowRate = new FlowRate(time.Hours, time.Minutes, time.Seconds);
                                    currentSession.PendingFlowRate.StartLineNumber = lineNumber;
                                }
                                else if (currentSession.PendingFlowRate.Time.Hours != time.Hours
                                           || currentSession.PendingFlowRate.Time.Minutes != time.Minutes
                                           || currentSession.PendingFlowRate.Time.Seconds != time.Seconds)
                                {
                                    // add pending flowrate and create new
                                    if (!flowRateFilter.HasValue || flowRateFilter <= currentSession.PendingFlowRate.TotalCallCount)
                                        currentSession.FlowRates.Add(currentSession.PendingFlowRate);

                                    currentSession.PendingFlowRate = new FlowRate(time.Hours, time.Minutes, time.Seconds);
                                    currentSession.PendingFlowRate.StartLineNumber = lineNumber;
                                }
                                var pendFlowRate = currentSession.PendingFlowRate;
                                pendFlowRate.TotalCallCount++;
                                pendFlowRate.PayloadByteCount += dataLength;
                                currentSession.TotalPayloadByteCount += dataLength;

                                // round trip determination
                                MethodInvokeRoundtrip methodInvokeReq = null;
                                MethodInvokeRoundtrip methodInvokeResp = null;
                                switch (message.Type)
                                {
                                    case MessageType.AsyncMethodInvokeRequest:

                                        if (isReceive)
                                        {
                                            currentSession.IncomingAsyncCallCount++;
                                            pendFlowRate.IncomingAsyncCallCount++;

                                            CheckReceiveRequestOrder(errors, currentSession, message);
                                        }
                                        else
                                        {
                                            currentSession.OutgoingAsyncCallCount++;
                                            pendFlowRate.OutgoingAsyncCallCount++;

                                            CheckSendRequestOrder(errors, currentSession, message);
                                        }

                                        break;
                                    case MessageType.MethodInvokeRequest:

                                        methodInvokeReq = new MethodInvokeRoundtrip();
                                        methodInvokeReq.IsReceive = isReceive;
                                        methodInvokeReq.Request = message;
                                        methodInvokeReq.RequestTime = time;

                                        if (isReceive)
                                        {
                                            currentSession.IncomingSyncCallCount++;
                                            currentSession.IncomingSyncCalls.Add(message.RequestId, methodInvokeReq);
                                            pendFlowRate.IncomingSyncCallCount++;

                                            CheckReceiveRequestOrder(errors, currentSession, message);
                                        }
                                        else
                                        {
                                            currentSession.OutgoingSyncCallCount++;
                                            currentSession.OutgoingSyncCalls.Add(message.RequestId, methodInvokeReq);
                                            pendFlowRate.OutgoingSyncCallCount++;

                                            CheckSendRequestOrder(errors, currentSession, message);
                                        }

                                        break;


                                    case MessageType.MethodInvokeResponse:

                                        if (isReceive)
                                        {
                                            if (currentSession.OutgoingSyncCalls.TryGetValue(message.RequestId, out methodInvokeResp))
                                            {
                                                methodInvokeResp.Response = message;
                                                methodInvokeResp.ResponseTime = time;

                                                if (roundTripTimeFilter.HasValue
                                                    && methodInvokeResp.RoundTripTime < roundTripTimeFilter)
                                                {
                                                    currentSession.OutgoingSyncCalls.Remove(message.RequestId);
                                                }

                                                if (methodInvokeResp.RoundTripTime.HasValue)
                                                {
                                                    if (currentSession.OutgoingSyncCallMinDuration is null || currentSession.OutgoingSyncCallMinDuration > methodInvokeResp.RoundTripTime)
                                                        currentSession.OutgoingSyncCallMinDuration = methodInvokeResp.RoundTripTime;

                                                    if (currentSession.OutgoingSyncCallMaxDuration is null || currentSession.OutgoingSyncCallMaxDuration < methodInvokeResp.RoundTripTime)
                                                        currentSession.OutgoingSyncCallMaxDuration = methodInvokeResp.RoundTripTime;

                                                    currentSession.OutgoingSyncCallTotalDuration += methodInvokeResp.RoundTripTime.Value;
                                                }
                                            }
                                            else
                                            {
                                                errors.AppendLine("Incoming method invoke response could not be assigned to a request: ");
                                                errors.AppendLine(line);
                                                errors.AppendLine();
                                            }
                                        }
                                        else
                                        {
                                            if (currentSession.IncomingSyncCalls.TryGetValue(message.RequestId, out methodInvokeResp))
                                            {
                                                methodInvokeResp.Response = message;
                                                methodInvokeResp.ResponseTime = time;

                                                if (roundTripTimeFilter.HasValue
                                                    && methodInvokeResp.RoundTripTime < roundTripTimeFilter)
                                                {
                                                    currentSession.IncomingSyncCalls.Remove(message.RequestId);
                                                }
                                            }
                                            else
                                            {
                                                errors.AppendLine("Outgoing method invoke response could not be assigned to a incoming request: ");
                                                errors.AppendLine(line);
                                                errors.AppendLine();
                                            }
                                        }

                                        break;
                                }


                                // handle file export
                                if (fileExort != null)
                                {
                                    if (openFileExportStreams == null)
                                        openFileExportStreams = new Dictionary<string, StreamWriter>();

                                    if (fileExort.MessageNames.Where(mn => mn.Equals(message.Name)).Any())
                                    {
                                        bool isConditionMatching = true;
                                        if (fileExort.Conditions != null)
                                        {
                                            foreach (var cond in fileExort.Conditions)
                                            {
                                                string condValue = GetJsonSimpleStringValue(dataPart, cond.Key);

                                                if (cond.Value != condValue)
                                                {
                                                    isConditionMatching = false;
                                                    break;
                                                }
                                            }
                                        }

                                        if (isConditionMatching)
                                        {
                                            string fileExportGroupValues = string.Concat(fileExort.GroupByKeys.Select(gk => GetJsonSimpleStringValue(dataPart, gk) + "_"));
                                            //string fileExportGroupValue = GetJsonSimpleStringValue(dataPart, fileExort.GroupByKey);

                                            StreamWriter currentExportWriter = null;
                                            if (openFileExportStreams.TryGetValue(fileExportGroupValues, out currentExportWriter) == false)
                                            {
                                                if (Directory.Exists(fileExort.ExportDirectory) == false)
                                                    Directory.CreateDirectory(fileExort.ExportDirectory);

                                                string conditions = string.Concat(fileExort.Conditions?.Select(cd => $"{cd.Key}-{cd.Value}_"));
                                                string groupKeyValues = string.Concat(fileExort.GroupByKeys?.Select(gk => $"{gk}-{GetJsonSimpleStringValue(dataPart, gk)}_"));

                                                string exportPath = Path.Combine(Path.GetFullPath(fileExort.ExportDirectory), $"{conditions}{groupKeyValues.TrimEnd('_')}.txt");

                                                if (File.Exists(exportPath))
                                                    File.Delete(exportPath);

                                                currentExportWriter = new StreamWriter(exportPath);

                                                openFileExportStreams.Add(fileExportGroupValues, currentExportWriter);
                                            }

                                            if (fileExort.SeparateOnKey != null)
                                            {
                                                string separateOnKeyValue = GetJsonSimpleStringValue(dataPart, fileExort.SeparateOnKey);

                                                if (separateOnKeyValue == fileExort.SeparateOnKeyValue)
                                                {
                                                    currentExportWriter.WriteLine(Environment.NewLine);
                                                    currentExportWriter.WriteLine(Environment.NewLine);
                                                }
                                            }

                                            ExportOnlyHandling(fileExort, line, isReceive, dataPart, currentExportWriter);

                                            if (methodInvokeReq != null && methodInvokeReq.ExportFileStream == null)
                                                methodInvokeReq.ExportFileStream = currentExportWriter;
                                        }

                                    }
                                    else if (methodInvokeResp != null
                                        && methodInvokeResp.ExportFileStream != null)
                                    {
                                        // write response as well
                                        ExportOnlyHandling(fileExort, line, isReceive, dataPart, methodInvokeResp.ExportFileStream);
                                        methodInvokeResp.ExportFileStream = null;
                                    }
                                }
                            }
                        }
                        else
                        {
                            errors.AppendLine("Invalid data row: " + line);
                        }
                    }
                    else
                    {
                        errors.AppendLine("Invalid data row! Could not be splitted into 4 parts: " + line);
                    }

                    lineNumber++;

                    if (progressPercentage != null
                        && (lineNumber % 30000) == 0)
                    {
                        double dblPercentage = 100d / totalFileSize * (double)streamReader.BaseStream.Position;
                        progressPercentage((int)dblPercentage);
                    }
                }

                if (openFileExportStreams != null)
                {
                    foreach (var fileStream in openFileExportStreams.Values)
                    {
                        fileStream.Close();
                    }
                }

                return sessions.Values.ToList<StreamSession>();
            }
        }

        private static void ExportOnlyHandling(FileExportFilter fileExort, string line, bool isReceive, string dataPart, StreamWriter currentExportWriter)
        {
            string output;
            if (fileExort.ExportOnlyKey != null)
            {
                string exportOnlyValue = GetJsonSimpleStringValue(dataPart, fileExort.ExportOnlyKey);

                output = (isReceive ? "< " : "> ") + exportOnlyValue;
            }
            else
                output = line;

            if (fileExort.ExportWithSpaceSequence.HasValue)
            {
                var list = Enumerable
                    .Range(0, output.Length / fileExort.ExportWithSpaceSequence.Value)
                    .Select(i => output.Substring(i * fileExort.ExportWithSpaceSequence.Value, fileExort.ExportWithSpaceSequence.Value));
                output = string.Join(" ", list);
            }

            currentExportWriter.WriteLine(output);
        }

        private static void CheckSendRequestOrder(StringBuilder errors, StreamSession currentSession, IGenericMessage message)
        {
            long expectedNextRequestId = currentSession.LastSendRequestId + 1;
            if (expectedNextRequestId != message.RequestId)
            {
                errors.AppendLine($"Unexpected send request id: {message.RequestId} - expected: {expectedNextRequestId} - stream is not continuous!");
            }
            currentSession.LastSendRequestId = message.RequestId;
        }

        private static void CheckReceiveRequestOrder(StringBuilder errors, StreamSession currentSession, IGenericMessage message)
        {
            long expectedNextRequestId = currentSession.LastReceiveRequestId + 1;
            if (expectedNextRequestId != message.RequestId)
            {
                errors.AppendLine($"Unexpected receive request id: {message.RequestId} - expected: {expectedNextRequestId} - stream is not continuous!");
            }
            currentSession.LastReceiveRequestId = message.RequestId;
        }


        /// <summary>
        /// Test helper method only !
        /// </summary>
        /// <param name="streamSessions"></param>
        public void ExtractSessionsFrom(IList<StreamSession> streamSessions)
        {
            List<StreamSession> filteredSessions = new List<StreamSession>();
            for (int sessionIndex = 0; sessionIndex < streamSessions.Count; sessionIndex++)
            {
                var session = streamSessions[sessionIndex];

                if (session.SessionInfo.Contains("<ip>"))
                {
                    if (session.SessionInfo.Contains("<SessionName>"))
                    {
                        filteredSessions.Add(session);
                    }
                }
            }

            HashSet<string> deviceIds = new HashSet<string>();
            foreach (var s in filteredSessions)
            {
                var firstCall = s.IncomingSyncCalls.FirstOrDefault();

                if (firstCall.Value != null
                    && firstCall.Value.Request != null)
                {
                    if (firstCall.Value.Request.Name == "<RequestMethodName>")
                    {
                        // collect data
                        //var valueIdStr = GetJsonSimpleStringValue(firstCall.Value.Request.Payload.ToString(), "ValueId");
                        //deviceIds.Add(valueIdStr);
                    }
                }
            }

            foreach (var item in deviceIds)
            {
                Debug.WriteLine(item);
            }
        }

        private const string AllSessionsTag = "<All Sessions>";

        public void MergeSessions(IList<StreamSession> streamSessions)
        {
            if (!streamSessions.Where(s => s.SessionInfo == AllSessionsTag).Any())
            {
                StreamSession mergeSession = new StreamSession(0, AllSessionsTag, TimeSpan.Zero, streamSessions.First().Format);

                long dummyRequestId = 1;
                for (int sessionIndex = 0; sessionIndex < streamSessions.Count; sessionIndex++)
                {
                    var session = streamSessions[sessionIndex];

                    mergeSession.IncomingAsyncCallCount += session.IncomingAsyncCallCount;
                    mergeSession.IncomingSyncCallCount += session.IncomingSyncCallCount;
                    foreach (var item in session.IncomingSyncCalls)
                    {
                        mergeSession.IncomingSyncCalls.Add(dummyRequestId, item.Value);
                        dummyRequestId++;
                    }
                    mergeSession.OutgoingAsyncCallCount += session.OutgoingAsyncCallCount;
                    mergeSession.OutgoingSyncCallCount += session.OutgoingSyncCallCount;
                    foreach (var item in session.OutgoingSyncCalls)
                    {
                        mergeSession.OutgoingSyncCalls.Add(dummyRequestId, item.Value);
                        dummyRequestId++;
                    }

                    // merge flow rates
                    foreach (var flowRate in session.FlowRates)
                    {
                        var existingFlowRate = (from fr in mergeSession.FlowRates
                                                where fr.Time == flowRate.Time
                                                select fr).FirstOrDefault();

                        if (existingFlowRate != null)
                        {
                            existingFlowRate.IncomingAsyncCallCount += flowRate.IncomingAsyncCallCount;
                            existingFlowRate.IncomingSyncCallCount += flowRate.IncomingSyncCallCount;
                            existingFlowRate.OutgoingAsyncCallCount += flowRate.OutgoingAsyncCallCount;
                            existingFlowRate.OutgoingSyncCallCount += flowRate.OutgoingSyncCallCount;
                            existingFlowRate.PayloadByteCount += flowRate.PayloadByteCount;
                            existingFlowRate.TotalCallCount += flowRate.TotalCallCount;

                            if (existingFlowRate.StartLineNumber > flowRate.StartLineNumber)
                            {
                                existingFlowRate.StartLineNumber = flowRate.StartLineNumber;
                            }
                        }
                        else
                        {
                            mergeSession.FlowRates.Add(flowRate);
                        }
                    }
                }
                streamSessions.Add(mergeSession);
            }
        }

        private static bool TryParseDataStreamLine(string[] lineParts, out TimeSpan time, out int sessionId, out bool isReceive)
        {
            if (!TimeSpan.TryParse(lineParts[0], out time))
            {
                sessionId = default(int);
                isReceive = default(bool);
                return false;
            }

            if (!int.TryParse(lineParts[1], out sessionId))
            {
                sessionId = default(int);
                isReceive = default(bool);
                return false;
            }

            if (lineParts[2] == "R")
            {
                isReceive = true;
            }
            else
            {
                isReceive = false;
            }

            return true;
        }


        private static IGenericMessage ParseMessage(string textMsg, RawMessageFormat format, StreamSession session, out int dataLength)
        {
            IGenericMessage message;
            if (format == RawMessageFormat.Binary)
            {
                byte[] binaryData = Convert.FromBase64String(textMsg);

                if (binarySerializer == null)
                {
                    binarySerializer = new BinaryMessageSerializer();
                    binarySerializer.Serializer.SerializeItemFilter = (IValueItem item) =>
                    {
                        // functional types not present > skip payload deserialize
                        if (item.Name == "Payload")
                            return false;
                        else
                            return true;
                    };
                }

                message = binarySerializer.DeserializeFromBytes(binaryData, binaryData.Length, null, session.SessionId);
                dataLength = binaryData.Length;
            }
            else
            {
                //{"Type":10,"RequestId":2,"Target":"BSAG.Xitaro.XBCI.Requester.Interface.Services.IRouterService","Name":"GetLatestSequenceNumbers()","Payload":[]}

                message = new GenericMessage();
                message.Type = (MessageType)short.Parse(GetJsonSimpleStringValue(textMsg, "Type"));
                message.RequestId = long.Parse(GetJsonSimpleStringValue(textMsg, "RequestId"));
                message.Name = GetJsonSimpleStringValue(textMsg, "Name");
                message.Target = GetJsonSimpleStringValue(textMsg, "Target");

                message.Payload = GetJsonSimpleStringValue(textMsg, "Payload");
                dataLength = textMsg.Length;
            }

            return message;
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

            char startChar = json[startIndex];
            if (startChar == CharQuotationMark)
            {
                startIndex++;

                // value ends with quotation mark
                endIndex = json.IndexOf(CharQuotationMark, startIndex);
            }
            else if (json[startIndex] == CharJsonArrayStart)
            {
                // json start array
                endIndex = json.IndexOf(CharJsonArrayEnd, startIndex);
            }
            else
            {
                // value ends with comma
                endIndex = json.IndexOf(Comma, startIndex);
            }

            if (endIndex == -1)
            {
                endIndex = json.IndexOf('}', startIndex);

                if (endIndex == -1)
                    endIndex = json.Length - 1;
            }

            return json.Substring(startIndex, endIndex - startIndex);
        }


        public void ExportFlowRateRows(StreamSession session, FlowRate flowRate, string targetPath)
        {
            using (StreamReader streamReader = new StreamReader(LastFilePath))
            {
                using (StreamWriter sw = new StreamWriter(targetPath))
                {
                    string startTimeStr = flowRate.Time.ToString();
                    bool allSessions = session.SessionId == 0;

                    string line = null;
                    long lineNumber = 1;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (flowRate.StartLineNumber <= lineNumber)
                        {
                            if (line.StartsWith(startTimeStr))
                            {
                                if (allSessions)
                                {
                                    sw.WriteLine(line);
                                }
                                else
                                {
                                    // get session id
                                    int firstSepIndex = line.IndexOf(FieldSeparator);
                                    int secondSepIndex = line.IndexOf(FieldSeparator, firstSepIndex + 1);

                                    if (firstSepIndex > 0 && secondSepIndex > 0)
                                    {
                                        string sessionIdStr = line.Substring(firstSepIndex + 1, secondSepIndex - firstSepIndex - 1);
                                        int sessionId = int.Parse(sessionIdStr);

                                        if (session.SessionId == sessionId)
                                        {
                                            sw.WriteLine(line);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        lineNumber++;
                    }
                }
            }
        }


        public void ExportSessionRows(StreamSession session, string targetPath)
        {
            using (StreamReader streamReader = new StreamReader(LastFilePath))
            {
                using (StreamWriter sw = new StreamWriter(targetPath))
                {
                    string line = null;
                    long lineNumber = 1;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        // get session id
                        int firstSepIndex = line.IndexOf(FieldSeparator);
                        int secondSepIndex = line.IndexOf(FieldSeparator, firstSepIndex + 1);

                        if (firstSepIndex > 0 && secondSepIndex > 0)
                        {
                            string sessionIdStr = line.Substring(firstSepIndex + 1, secondSepIndex - firstSepIndex - 1);
                            int sessionId = int.Parse(sessionIdStr);

                            if (session.SessionId == sessionId)
                            {
                                sw.WriteLine(line);
                            }
                        }

                        lineNumber++;
                    }
                }
            }
        }

        #endregion
    }
}
