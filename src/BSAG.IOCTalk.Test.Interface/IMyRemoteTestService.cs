using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface IMyRemoteTestService
    {
        void SendMessage(string data);


        void SameNameTest(int id);

        void SameNameTest(int[] ids);
    }
}
