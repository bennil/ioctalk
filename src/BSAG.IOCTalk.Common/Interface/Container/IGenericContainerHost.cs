using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Session;
using BSAG.IOCTalk.Common.Interface.Session;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    /// <summary>
    /// Specifies a dependency injection container host including a communication binding.
    /// </summary>
    public interface IGenericContainerHost
    {
        /// <summary>
        /// Gets the dependency injection container instance.
        /// </summary>
        object DIContainer { get; }

        /// <summary>
        /// Gets the name of the container host.
        /// </summary>
        string Name { get; }


        /// <summary>
        /// Initalizes the generic communication.
        /// </summary>
        /// <param name="communicationService">The communication service.</param>
        void InitGenericCommunication(IGenericCommunicationService communicationService);

        /// <summary>
        /// Creates the session contract instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        IContract CreateSessionContractInstance(ISession session);

        /// <summary>
        /// Gets the interface implementation instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns></returns>
        object GetInterfaceImplementationInstance(ISession session, string interfaceType);

        /// <summary>
        /// Gets the interface implementation type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <returns></returns>
        Type GetInterfaceImplementationType(string interfaceType);

        /// <summary>
        /// Gets the session by the given service import instance.
        /// </summary>
        /// <param name="serviceObjectInstance">The service object instance.</param>
        /// <returns></returns>
        ISession GetSessionByServiceInstance(object serviceObjectInstance);

        /// <summary>
        /// Gets the type of the exposed sub interface.
        /// </summary>
        /// <param name="sourceType">Type of the concrete source.</param>
        /// <returns>Returns null if no sub interface is exposed</returns>
        Type GetExposedSubInterfaceForType(Type sourceType);

        /// <summary>
        /// Registers the type mapping for an exposed sub interface.
        /// </summary>
        /// <param name="interfaceType">Type of the exposed interface.</param>
        /// <param name="sourceType">Type of the concrete source.</param>
        void RegisterExposedSubInterfaceForType(Type interfaceType, Type sourceType);


        bool IsAsyncVoidRemoteInvoke(Type type, string methodName);
    }
}
