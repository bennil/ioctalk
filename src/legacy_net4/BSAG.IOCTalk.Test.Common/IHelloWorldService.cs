using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSAG.IOCTalk.Test.Common
{
    public interface IHelloWorldService
    {
        /// <summary>
        /// Say hello to the world
        /// </summary>
        /// <param name="name">Your name</param>
        /// <returns></returns>
        string HelloWorld(string name);
    }
}
