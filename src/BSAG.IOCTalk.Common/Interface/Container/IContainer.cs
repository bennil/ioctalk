using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    public interface ITalkContainer
    {
        bool TryGetExport(Type type, Type injectTargetType, out object instance);
    }
}
