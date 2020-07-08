using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Communication.PersistentQueue.Transaction
{
    public class TrxResendActionUseReturnValue
    {
        public TrxResendActionUseReturnValue(string applyToParameterName)
        {
            this.ApplyToParameterName = applyToParameterName;
        }

        public string ApplyToParameterName { get; set; }
    }
}
