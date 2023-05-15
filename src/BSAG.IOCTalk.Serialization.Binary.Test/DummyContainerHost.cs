using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Common.Interface.Container;
using BSAG.IOCTalk.Common.Interface.Session;
using BSAG.IOCTalk.Serialization.Binary.Test.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.Test
{
    internal class DummyContainerHost : IGenericContainerHost
    {
        public object DIContainer => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public IContract CreateSessionContractInstance(ISession session)
        {
            throw new NotImplementedException();
        }

        public Type GetExposedSubInterfaceForType(Type sourceType)
        {
            throw new NotImplementedException();
        }

        public object GetInterfaceImplementationInstance(ISession session, string interfaceType)
        {
            throw new NotImplementedException();
        }

        public Type GetInterfaceImplementationType(string interfaceType)
        {
            if (interfaceType == typeof(ITestItem).FullName)
                return typeof(TestItem);

            throw new NotSupportedException();
        }

        public ISession GetSessionByServiceInstance(object serviceObjectInstance)
        {
            throw new NotImplementedException();
        }

        public void InitGenericCommunication(IGenericCommunicationService communicationService)
        {
            throw new NotImplementedException();
        }

        public bool IsAsyncVoidRemoteInvoke(Type type, string methodName)
        {
            throw new NotImplementedException();
        }

        public void RegisterExposedSubInterfaceForType(Type interfaceType, Type sourceType)
        {
            throw new NotImplementedException();
        }
    }
}
