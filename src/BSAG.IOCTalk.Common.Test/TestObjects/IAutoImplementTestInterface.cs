using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Common.Test.TestObjects
{
    public interface IAutoImplementTestInterface
    {
        void OnVersionApplied(int newVersion);

        string[] GetInstalledApps();


        Guid StartFileUploadSession(string upload);

        void PushPart(Guid sId, int seqNo, byte[] data);

        /// <summary>
        /// Completes the upload and returns the target path
        /// </summary>
        /// <param name="sId"></param>
        /// <returns></returns>
        string CompleteUpload(Guid sId);


        void InstallOrUpdateLocalApp(string installConfig);
    }
}
