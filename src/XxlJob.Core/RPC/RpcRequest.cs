using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xxl.job.core.rpc.codec
{
    public class RpcRequest
    {
        /// <summary>
        /// xxl-job 1.9.2及以前版本中的字段
        /// </summary>
        public string serverAddress;

        /// <summary>
        /// xxl-job 2.0.0版本新增的字段
        /// </summary>
        public string requestId;
        public string version;

        /// <summary>
        /// xxl-job 1.9.1+都有的字段
        /// </summary>
        public long createMillisTime;
        public string accessToken;
        public string className;
        public string methodName;
        public ArrayList parameterTypes;
        public ArrayList parameters;

        public virtual RpcResponse CreateRpcResponse()
        {
            var response = new RpcResponse();
            return response;
        }
    }
}

namespace com.xxl.rpc.remoting.net.@params
{
    using com.xxl.job.core.rpc.codec;

    public class XxlRpcRequest : RpcRequest
    {
        public override RpcResponse CreateRpcResponse()
        {
            var response = new XxlRpcResponse();
            response.requestId = this.requestId;
            return response;
        }
    }
}