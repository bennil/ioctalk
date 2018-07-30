using BSAG.IOCTalk.Test.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BSAG.IOCTalk.Test.Common.Service
{
    public class MultipleLocalImportsService : IMultipleLocalImportsService
    {
        private IEnumerable<IMultipleImplementation> localImplementations;
        private IMultipleImplementation[] localImplementationsArray;

        public MultipleLocalImportsService(IEnumerable<IMultipleImplementation> localImplementations, IMultipleImplementation[] localImplementationsArray)
        {
            this.localImplementations = localImplementations;
            this.localImplementationsArray = localImplementationsArray;

            var first1 = localImplementations.First();
            var first2 = localImplementationsArray[0];

            if (first1.GetHashCode() != first2.GetHashCode())
            {
                throw new InvalidOperationException("Unexpected new service instance!");
            }
        }

        public IEnumerable<IMultipleImplementation> LocalImplementations
        {
            get { return localImplementations; }
        }

        IMultipleImplementation[] IMultipleLocalImportsService.LocalImplementations => localImplementationsArray;
    }
}
