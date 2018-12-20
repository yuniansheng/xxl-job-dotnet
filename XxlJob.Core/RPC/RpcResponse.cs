using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xxl.job.core.rpc.codec
{
    public class RpcResponse
    {
        public string requestId;

        public string error;

        public object result;

        public bool IsError { get { return error != null; } }
    }
}
