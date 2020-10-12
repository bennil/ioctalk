using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue.Transaction
{
    public class ResendTransaction : IDisposable
    {
        private List<(Stream FileStream, long PositionIndex)> sendIndicatorStreamPositions;

        public ResendTransaction()
        {
        }

        public List<TrxContextValue> ContextValues { get; private set; }


        public List<(Stream FileStream, long PositionIndex)> SendIndicatorStreamPositions => sendIndicatorStreamPositions;

        public void SetTransactionValue(Type type, string name, object value)
        {
            if (ContextValues == null)
                ContextValues = new List<TrxContextValue>();

            ContextValues.Add(new TrxContextValue { Type = type, Name = name, Value = value });
        }


        public void AddSendIndicatorPosition(Stream stream, long positionIndex)
        {
            if (sendIndicatorStreamPositions == null)
                sendIndicatorStreamPositions = new List<(Stream FileStream, long PositionIndex)>();

            sendIndicatorStreamPositions.Add((stream, positionIndex));
        }

        public void Dispose()
        {
            ContextValues = null;
        }

        internal void FlagTransactionMethodsSuccess()
        {
            lock (PersistentClientCommunicationHost.syncLock)
            {
                Stream changedPosStream = null;
                long changedStreamOldPosition = 0;
                foreach (var item in sendIndicatorStreamPositions)
                {
                    var fs = item.FileStream;

                    // pos management
                    if (changedPosStream == null)
                    {
                        changedPosStream = fs;
                        changedStreamOldPosition = fs.Position;
                    }
                    else if (changedPosStream != fs)
                    {
                        // reset last stream to old position
                        changedPosStream.Seek(changedStreamOldPosition, SeekOrigin.Begin);
                        changedPosStream = fs;
                        changedStreamOldPosition = fs.Position;
                    }

                    // move file stream to sent flag
                    fs.Seek(item.PositionIndex, SeekOrigin.Begin);

                    // mark transaction method as successfully sent
                    fs.WriteByte(PersistentClientCommunicationHost.AlreadySentByte);    // sent = true
                    fs.Flush();
                }

                if (changedPosStream != null)
                {
                    // reset stream to old position
                    changedPosStream.Seek(changedStreamOldPosition, SeekOrigin.Begin);
                }
            }
        }

        internal void ClearSendIndicatorPositions()
        {
            sendIndicatorStreamPositions?.Clear();
        }
    }
}
