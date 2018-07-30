using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MyLocalService : IMyLocalService
    {
        public MyLocalService()
        {
            InstanceCount++;
        }

        public static int InstanceCount { get; set; }

        public void RandomMethod()
        {
            throw new NotImplementedException();
        }
    }
}
