using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue.Transaction
{
    /// <summary>
    /// The transaction definition specifies all methods combined as transaction context.
    /// </summary>
    public class TransactionDefinition
    {
        private ResendTransaction currentTrx;

        public TransactionDefinition(string name)
        {
            this.Name = name;
            this.Methods = new List<PersistentMethod>();
        }

        public string Name { get; private set; }

        public PersistentMethod BeginTransactionMethod { get; set; }

        public PersistentMethod CommitTransactionMethod { get; set; }

        public List<PersistentMethod> Methods { get; set; }

        public ResendTransaction CurrentTransaction
        {
            get { return currentTrx; }
        }
        
        internal ResendTransaction BeginTransaction()
        {
            if (currentTrx != null)
                throw new NotSupportedException("Only one open transaction supported");

            currentTrx = new ResendTransaction();
            return currentTrx;
        }

        internal void CommitTransaction()
        {
            if (currentTrx.SendIndicatorStreamPositions != null
                && currentTrx.SendIndicatorStreamPositions.Count > 0)
            {
                // commit succesfully > mark all transaction send flags as sent
                currentTrx.FlagTransactionMethodsSuccess();
            }

            currentTrx.Dispose();
            currentTrx = null;
        }
    }
}
