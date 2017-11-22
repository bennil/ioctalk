using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BSAG.IOCTalk.Common.Interface.Communication;
using System.Xml.Linq;
using BSAG.IOCTalk.Common.Reflection;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Config;

namespace BSAG.IOCTalk.Communication.Common.Factory
{
    /// <summary>
    /// IOC-Talk communication loader
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Author: blink, created at 1/23/2015 3:54:33 PM.
    ///  </para>
    /// </remarks>
    public static class IOCTalkLoader
    {
        #region fields

        public const string AppConfigSectionName = "IOCTalk";
        public const string SessionContractXmlName = "SessionContract";
        public const string CommunicationXmlName = "Communication";
        public const string ContainerXmlName = "Container";
        public const string SerializerXmlName = "Serializer";

        public const string TypeAttributeName = "type";
        #endregion

        #region properties


        #endregion

        #region methods


        /// <summary>
        /// Initializes the IOC-Talk communication using the application configuration.
        /// </summary>
        /// <returns></returns>
        public static IList<IGenericCommunicationService> InitFromAppConfig()
        {
            List<IGenericCommunicationService> communicationServices = new List<IGenericCommunicationService>();

            XDocument config = (XDocument)System.Configuration.ConfigurationManager.GetSection(AppConfigSectionName);

            if (config != null)
            {
                foreach (var sessionContract in config.Root.Elements(SessionContractXmlName))
                {
                    Type sessionContractType = GetTypeAttribute(sessionContract);



                    var communicationConfig = sessionContract.Element("Communication");
                    if (communicationConfig == null)
                        throw new KeyNotFoundException(string.Format("The XML element \"{0}\" attribute was not found in AppConfig section \"{1}\"!", CommunicationXmlName, sessionContract));

                    // load communication
                    Type communicationControllerType = GetTypeAttribute(communicationConfig);
                    if (!typeof(IGenericCommunicationService).IsAssignableFrom(communicationControllerType))
                    {
                        throw new InvalidCastException(string.Format("The communication type \"{0}\" must implement the \"{1}\" interface!", communicationControllerType.FullName, typeof(IGenericCommunicationService).FullName));
                    }

                    IGenericCommunicationService communicationService = (IGenericCommunicationService)TypeService.CreateInstance(communicationControllerType);
                    
                    SetXmlConfig(communicationConfig, communicationService);

                    // set serializer
                    var serializerElement = sessionContract.Element(SerializerXmlName);
                    if (serializerElement != null)
                    {
                        var serializerTypeAttr = serializerElement.Attribute(TypeAttributeName);

                        communicationService.SerializerTypeName = serializerTypeAttr.Value;
                    }

                    // load container
                    var containerConfig = sessionContract.Element("Container");
                    if (containerConfig == null)
                        throw new KeyNotFoundException(string.Format("The XML element \"{0}\" attribute was not found in AppConfig section \"{1}\"!", ContainerXmlName, sessionContract));

                    Type containerHostType = GetTypeAttribute(containerConfig);
                    if (!typeof(IGenericContainerHost).IsAssignableFrom(containerHostType))
                    {
                        throw new InvalidCastException(string.Format("The container host type \"{0}\" must implement the \"{1}\" interface!", containerHostType.FullName, typeof(IGenericContainerHost).FullName));
                    }

                    if (containerHostType.IsGenericType
                        && containerHostType.GetGenericArguments().Length == 1)
                    {
                        Type sessionContractContainerHostType = containerHostType.MakeGenericType(sessionContractType);

                        IGenericContainerHost containerHost = (IGenericContainerHost)TypeService.CreateInstance(sessionContractContainerHostType);

                        SetXmlConfig(containerConfig, containerHost);

                        containerHost.InitGenericCommunication(communicationService);
                    }
                    else
                    {
                        throw new InvalidCastException(string.Format("Container host type \"{0}\" must be generic with one argument!", containerHostType.FullName));
                    }

                    communicationService.Init();
                    communicationServices.Add(communicationService);
                }
            }
            else
            {
                throw new NullReferenceException("AppConfig section \"" + AppConfigSectionName + "\" not found!");
            }

            return communicationServices;
        }


        private static Type GetTypeAttribute(XElement element)
        {
            var typeAttribute = element.Attribute(TypeAttributeName);

            if (typeAttribute == null || string.IsNullOrEmpty(typeAttribute.Value))
                throw new KeyNotFoundException(string.Format("The \"{0}\" attribute was not found in AppConfig section \"{1}\"!", TypeAttributeName, element));

            Type type;
            if (!TypeService.TryGetTypeByName(typeAttribute.Value, out type))
            {
                throw new TypeLoadException("Type \"" + typeAttribute.Value + "\" not found!");
            }
            return type;
        }


        private static void SetXmlConfig(XElement xmlConfig, object item)
        {
            if (item is IXmlConfig)
            {
                ((IXmlConfig)item).Config = new XDocument(xmlConfig);
            }
        }

        #endregion
    }
}
