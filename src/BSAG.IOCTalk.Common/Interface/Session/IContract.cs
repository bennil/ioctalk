using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Session
{
    public interface IContract
    {
        bool TryGetSessionInstance(Type type, out object instance);

        bool TryGetSessionInstance<InterfType>(out InterfType instance);

        InterfType GetSessionInstance<InterfType>();

        object[] RemoteServices { get; }

        object[] LocalServices { get; }
    }
}
