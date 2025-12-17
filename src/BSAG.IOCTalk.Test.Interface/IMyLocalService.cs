using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface IMyLocalService
    {
        void RandomMethod();

        Task RandomMethodAsync();

        void DataMethod(int id, DateTime time, string data);
    }
}
