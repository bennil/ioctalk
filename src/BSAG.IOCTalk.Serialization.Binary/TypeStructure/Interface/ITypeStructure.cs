using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bond.IO.Safe;

namespace BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface
{
    public interface ITypeStructure : IValueItem, IObjectType, ITypePrefix
    {
        /// <summary>
        /// Gets the structure items.
        /// </summary>
        /// <value>The items.</value>
        IList<IValueItem> Items { get; set; }

    }
}
