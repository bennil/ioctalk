using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue.Transaction
{
    public class ResendTransaction : IDisposable
    {
        public ResendTransaction()
        {
        }

        public List<TrxContextValue> ContextValues { get; set; }


        public void SetTransactionValue(Type type, string name, object value)
        {
            if (ContextValues == null)
                ContextValues = new List<TrxContextValue>();

            ContextValues.Add(new TrxContextValue { Type = type, Name = name, Value = value });
        }

        public void Dispose()
        {
            ContextValues = null;
        }
    }
}
