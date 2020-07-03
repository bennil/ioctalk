using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue.Transaction
{
    public class TrxContextValue
    {
        public Type Type { get; set; }

        public string Name { get; set; }

        public object Value { get; set; }
    }
}
