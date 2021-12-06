using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Reflection
{
    public delegate object CreateInstanceHandler(Type type, string parameterName, Type injectTargetType, List<Type> pendingCreateList);
}
