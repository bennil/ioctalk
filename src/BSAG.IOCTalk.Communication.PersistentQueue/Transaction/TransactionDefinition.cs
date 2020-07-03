﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue.Transaction
{
    /// <summary>
    /// todo: incomplete transaction implementation > current implementation only transaction context value focused!
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
            // todo: set sent flag in files
            currentTrx.Dispose();
            currentTrx = null;
        }
    }
}
