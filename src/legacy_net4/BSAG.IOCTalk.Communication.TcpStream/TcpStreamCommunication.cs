using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Communication.Raw;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Communication.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Serialization.Binary.Stream;
using BSAG.IOCTalk.Serialization.Binary;

namespace BSAG.IOCTalk.Communication.TcpStream
{
    public class TcpStreamCommunication : TcpCommunicationController
    {
        private Type streamSerializerType;


        public override void Init()
        {
            this.CustomCreateSessionHandler = CreateStreamSession;

            base.Init();
        }

        private ISession CreateStreamSession(IGenericCommunicationService communicationSerivce, int sessionId, string description)
        {
            //(IMessageStreamSerializer)TypeService.CreateInstance(streamSerializerType);
            BinaryMessageSerializer serializer = new BinaryMessageSerializer();
            serializer.RegisterContainerHost(communicationSerivce.ContainerHost);
            return new StreamSession(communicationSerivce, sessionId, description, serializer);
        }

        protected override void SubscribeCommunicationEvents(AbstractTcpCom tcpComm)
        {
            base.SubscribeCommunicationEvents(tcpComm);

            tcpComm.RawMessageReceivedDelegate = OnBinaryRawMessageReceived;
        }


        /// <summary>
        /// Called when [raw message received].
        /// </summary>
        /// <param name="rawMessage">The raw message.</param>
        public void OnBinaryRawMessageReceived(IRawMessage rawMessage)
        {
            //this.ProcessReceivedMessageBytes(rawMessage.SessionId, rawMessage.Data);

            try
            {
                ISession session;
                if (!sessionDictionary.TryGetValue(rawMessage.SessionId, out session))
                {
                    // session terminated -> ignore packets
                    if (logDataStream)
                    {
                        dataStreamLogger.LogStreamMessage(rawMessage.SessionId, true, DismissInvalidSessionMessageLogTag + Encoding.UTF8.GetString(rawMessage.Data, 0, rawMessage.Length));
                    }
                    return;
                }
                StreamSession streamSession = (StreamSession)session;

                //if (logDataStream)
                //{
                //    dataStreamLogger.LogStreamMessage(rawMessage.SessionId, true, messageBytes);
                //}
                //var reader = new StreamReader(rawMessage.Data, rawMessage.Length);
                var reader = streamSession.Reader;
                reader.UpdateBuffer(rawMessage.Data, 0, rawMessage.Length);

                IGenericMessage message = streamSession.StreamSerializer.Deserialize(reader, streamSession);

                ProcessReceivedMessage(session, message);
            }
            catch (Exception ex)
            {
                logger.Error(string.Format("Unexpected error during message processing! Message: \"{0}\" \n Exception: {1}", Encoding.UTF8.GetString(rawMessage.Data, 0, rawMessage.Length), ex.ToString()));
            }
        }

        public override void SendMessage(IGenericMessage message, int receiverSessionId, object context)
        {
            ISession session;
            if (sessionDictionary.TryGetValue(receiverSessionId, out session))
            {
                StreamSession streamSession = (StreamSession)session;
                streamSession.Writer.Reset();
                streamSession.StreamSerializer.Serialize(streamSession.Writer, message, context);
                //byte[] encapsulatedMessageBytes = AbstractTcpCom.CreateMessage(serializer.MessageFormat, msgBytes);
                //todo: msg formate etc
                byte[] encapsulatedMessageBytes = AbstractTcpCom.CreateMessage(RawMessageFormat.Binary, streamSession.Writer.Data.ToArray());

                communication.Send(encapsulatedMessageBytes, receiverSessionId);
            }
        }

        //public override void RegisterContainerHost(IGenericContainerHost containerHost)
        //{
        //    base.RegisterContainerHost(containerHost);

        //    if (Serializer is IMessageStreamSerializer)
        //    {
        //        streamSerializerType = Serializer.GetType();
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException($"Defined serializer \"{Serializer.GetType().FullName}\" does not implement {typeof(IMessageStreamSerializer).FullName}!");
        //    }
        //}

    }
}
