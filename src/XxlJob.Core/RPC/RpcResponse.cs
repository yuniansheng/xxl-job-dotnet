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

        public virtual string Error { get => error; set => error = value; }
    }
}

namespace com.xxl.rpc.remoting.net.@params
{
    using com.xxl.job.core.rpc.codec;

    public class XxlRpcResponse : RpcResponse
    {
        public string errorMsg;

        public override string Error { get => errorMsg; set => errorMsg = value; }
    }
}