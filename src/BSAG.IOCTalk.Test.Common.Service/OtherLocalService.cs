using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class OtherLocalService : IOtherLocalService
    {
        private IMyLocalService localService;

        public OtherLocalService(IMyLocalService localService)
        {
            InstanceCount++;

            this.localService = localService;
        }

        public static int InstanceCount { get; set; }


        public void SomeMethod()
        {
            throw new NotImplementedException();
        }
    }
}
