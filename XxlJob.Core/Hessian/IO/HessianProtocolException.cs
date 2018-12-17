using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Exception for faults when the fault doesn't return a java exception.
    /// This exception is required for MicroHessianInput.
    /// </summary>
    public class HessianProtocolException : IOException
    {
        public HessianProtocolException()
        {
        }

        public HessianProtocolException(string message) : base(message)
        {
        }

        public HessianProtocolException(string message, int hresult) : base(message, hresult)
        {
        }

        public HessianProtocolException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HessianProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}