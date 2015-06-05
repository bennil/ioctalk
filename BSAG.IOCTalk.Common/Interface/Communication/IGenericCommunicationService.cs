using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Common.Interface.Reflection;
using BSAG.IOCTalk.Common.Interface.Logging;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Specifies an generic communication service interface.
    /// </summary>
    /// <remarks>
    /// Author(s): Benjamin Link
    /// created on: 2013-07-09
    /// </remarks>
    public interface IGenericCommunicationService
    {
        #region IGenericCommunicationService events
        // ----------------------------------------------------------------------------------------
        // IGenericCommunicationService events
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Occurs when a session is created.
        /// </summary>
        event EventHandler<SessionEventArgs> SessionCreated;


        /// <summary>
        /// Occurs when a session is terminated.
        /// </summary>
        event EventHandler<SessionEventArgs> SessionTerminated;

        // ----------------------------------------------------------------------------------------
        #endregion

        #region IGenericCommunicationService properties
        // ----------------------------------------------------------------------------------------
        // IGenericCommunicationService properties
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the container host.
        /// </summary>
        /// <value>
        /// The container host.
        /// </value>
        IGenericContainerHost ContainerHost { get; }

        /// <summary>
        /// Gets or sets the name of the serializer type.
        /// </summary>
        /// <value>
        /// The name of the serializer type.
        /// </value>
        string SerializerTypeName { get; set; }

        /// <summary>
        /// Gets the message serializer.
        /// </summary>
        IGenericMessageSerializer Serializer { get; }

        /// <summary>
        /// Gets or sets the name of the logger type.
        /// </summary>
        /// <value>
        /// The name of the logger type.
        /// </value>
        string LoggerTypeName { get; set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        ILogger Logger { get; }


        /// <summary>
        /// Gets or sets a value indicating whether [log data stream].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [log data stream]; otherwise, <c>false</c>.
        /// </value>
        bool LogDataStream { get; set; }

        /// <summary>
        /// Gets or sets the name of the data stream logger type.
        /// </summary>
        /// <value>
        /// The name of the data stream logger type.
        /// </value>
        string DataStreamLoggerTypeName { get; set; }

        /// <summary>
        /// Gets the data stream logger.
        /// </summary>
        IDataStreamLogger DataStreamLogger { get; }


        /// <summary>
        /// Gets the client sessions.
        /// </summary>
        ISession[] ClientSessions { get; }


        /// <summary>
        /// Gets or sets the custom create session handler.
        /// </summary>
        /// <value>
        /// The custom create session handler.
        /// </value>
        CreateSessionHandler CustomCreateSessionHandler { get; set; }

        // ----------------------------------------------------------------------------------------
        #endregion

        #region IGenericCommunicationService methods
        // ----------------------------------------------------------------------------------------
        // IGenericCommunicationService methods
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Inits the communication service.
        /// </summary>
        void Init();


        /// <summary>
        /// Communication service shutdown
        /// </summary>
        void Shutdown();


        /// <summary>
        /// Registers the container host.
        /// </summary>
        /// <param name="containerHost">The container host.</param>
        void RegisterContainerHost(IGenericContainerHost containerHost);



        /// <summary>
        /// Invokes a remote interface method by a given lambda method expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        object InvokeMethod<T>(object source, Expression<Action<T>> expression);

        /// <summary>
        /// Invokes a remote interface method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        object InvokeMethod(object source, MethodInfo method, object[] parameters);

        /// <summary>
        /// Invokes a remote interface method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="invokeInfo">The invoke info.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        object InvokeMethod(object source, IInvokeMethodInfo invokeInfo, object[] parameters);

        // ----------------------------------------------------------------------------------------
        #endregion
    }
  
}
