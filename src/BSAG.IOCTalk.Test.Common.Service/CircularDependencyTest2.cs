using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class CircularDependencyTest2 : ICircularDependencyTest2
    {
        /// <summary>
        /// Mutual injection of ICircularDependencyTest 1 and 2 - results in a circular reference - for unit test purpose only
        /// </summary>
        /// <param name="other"></param>
        public CircularDependencyTest2(ICircularDependencyTest1 other)
        {
        }

        public void Bar()
        {
        }
    }
}
