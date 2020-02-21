using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Interface.Container
{
    public interface IContainerSharedByType
    {
        /// <summary>
        /// Gets the local shared instances by type
        /// </summary>
        IDictionary<Type, object> SharedLocalInstances { get; }

        bool TryGetCachedLocalExport(Type type, out object instance);
    }
}
