using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class CircularDependencyTest1 : ICircularDependencyTest1
    {
        /// <summary>
        /// Mutual injection of ICircularDependencyTest 1 and 2 - results in a circular reference - for unit test purpose only
        /// </summary>
        /// <param name="other"></param>
        public CircularDependencyTest1(ICircularDependencyTest2 other)
        {
        }

        public void Foo()
        {
        }
    }
}
