using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BSAG.IOCTalk.Common.Exceptions
{
    public class CircularServiceReferenceException : Exception
    {
        public CircularServiceReferenceException()
        {
        }

        public CircularServiceReferenceException(string message) : base(message)
        {
        }

        public CircularServiceReferenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CircularServiceReferenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public CircularServiceReferenceException(List<Type> pendingTypeCreateList, Type circularNodeType)
            :base(CreateErrorMessage(pendingTypeCreateList, circularNodeType))
        {
        }

        private static string CreateErrorMessage(List<Type> pendingTypeCreateList, Type circularNodeType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Circular service constructor injection found! Injection chain types: ");
            sb.Append(string.Join(" > ", pendingTypeCreateList.Select(pt => pt.FullName).ToArray()));
            sb.Append(" > ");
            sb.Append(circularNodeType.FullName);

            return sb.ToString();
        }
    }
}
