using BSAG.IOCTalk.Communication.PersistentQueue.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue
{
    public class PersistentMethod
    {
        public PersistentMethod(Type interfaceType, string methodName)
        {
            this.InterfaceType = interfaceType;
            this.MethodName = methodName;
        }

        public Type InterfaceType { get; private set; }

        public string MethodName { get; private set; }

        public TransactionDefinition Transaction { get; private set; }

        public TrxResendActionUseReturnValue TransactionResendAction { get; set; }

        public PersistentMethod RegisterTransactionBegin(TransactionDefinition trxDef)
        {
            if (this.Transaction != null)
                throw new InvalidOperationException("Transaction definition already assigned");

            if (trxDef.BeginTransactionMethod != null)
                throw new InvalidOperationException("Begin transaction method already assigned!");

            trxDef.BeginTransactionMethod = this;

            trxDef.Methods.Add(this);

            this.Transaction = trxDef;

            return this;
        }

        public PersistentMethod RegisterTransaction(TransactionDefinition trxDef)
        {
            if (this.Transaction != null)
                throw new InvalidOperationException("Transaction definition already assigned");

            trxDef.Methods.Add(this);
         
            this.Transaction = trxDef;

            return this;
        }

        public PersistentMethod RegisterTransactionCommit(TransactionDefinition trxDef)
        {
            if (this.Transaction != null)
                throw new InvalidOperationException("Transaction definition already assigned");

            if (trxDef.CommitTransactionMethod != null)
                throw new InvalidOperationException("Commit transaction method already assigned!");

            trxDef.CommitTransactionMethod = this;

            trxDef.Methods.Add(this);

            this.Transaction = trxDef;

            return this;
        }


        public PersistentMethod RegisterResendAction(TrxResendActionUseReturnValue resendAction)
        {
            this.TransactionResendAction = resendAction;
            return this;
        }
    }
}
