using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace BSAG.IOCTalk.Test.Common.Service.MEF
{
    /// <summary>
    /// Hello World Service implementation
    /// </summary>
    [Export(typeof(IHelloWorldService))]
    public class HelloWorldService : IHelloWorldService
    {
        /// <summary>
        /// Say hello to the world
        /// </summary>
        /// <param name="name">Your name</param>
        /// <returns></returns>
        public string HelloWorld(string name)
        {
            return string.Format("Hi {0}", name);
        }
    }
}
