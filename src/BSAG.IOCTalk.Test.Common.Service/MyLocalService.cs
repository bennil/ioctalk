using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MyLocalService : IMyLocalService
    {
        public MyLocalService()
        {
            InstanceCount++;
        }

        public static int InstanceCount { get; set; }

        public void DataMethod(int id, DateTime time, string data)
        {
            throw new NotImplementedException();
        }

        public void RandomMethod()
        {
            throw new NotImplementedException();
        }

        public Task RandomMethodAsync()
        {
            throw new NotImplementedException();
        }
    }
}
