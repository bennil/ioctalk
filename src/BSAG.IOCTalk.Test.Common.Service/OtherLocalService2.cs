using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class OtherLocalService2 : IOtherLocalService2
    {
        private IMyLocalService localService;
        private IOtherLocalService otherLocalService1;

        public OtherLocalService2(IMyLocalService localService, IOtherLocalService otherLocalService)
        {
            InstanceCount++;

            this.localService = localService;
            this.otherLocalService1 = otherLocalService;
        }

        public static int InstanceCount { get; set; }

        public void SomeMethod()
        {
            throw new NotImplementedException();
        }
    }
}
