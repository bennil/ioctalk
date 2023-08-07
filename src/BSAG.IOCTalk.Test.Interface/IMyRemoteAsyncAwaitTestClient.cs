using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Test.Interface
{
    public interface IMyRemoteAsyncAwaitTestClient
    {
        Task<IDataTransferTest> GetObjectDataID10Async();

        Task<IMapTestMainInterface> GetObjectData();

        Task<IMapTestMainInterface> GetObjectData2();
    }
}
