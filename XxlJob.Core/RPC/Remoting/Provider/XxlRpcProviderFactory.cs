using com.xxl.job.core.rpc.codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XxlJob.Core.Util;

namespace XxlJob.Core.RPC.Remoting.Provider
{
    internal class XxlRpcProviderFactory
    {
        private Dictionary<string, object> serviceData;
        private string accessToken;

        public XxlRpcProviderFactory()
        {
            this.serviceData = new Dictionary<string, object>();
        }

        public void InitConfig(string accessToken)
        {
            this.accessToken = accessToken;
        }

        public void AddService(string iface, string version, object serviceBean)
        {
            var serviceKey = MakeServiceKey(iface, version);
            serviceData.Add(serviceKey, serviceBean);
        }

        private string MakeServiceKey(string iface, string version)
        {
            string serviceKey = iface;
            if (!string.IsNullOrWhiteSpace(version))
            {
                serviceKey += "#" + version;
            }
            return serviceKey;
        }

        public RpcResponse InvokeService(RpcRequest xxlRpcRequest)
        {
            //  make response
            RpcResponse xxlRpcResponse = new RpcResponse();
            xxlRpcResponse.requestId = xxlRpcRequest.requestId;

            // match service bean
            var serviceKey = MakeServiceKey(xxlRpcRequest.className, xxlRpcRequest.version);
            object serviceBean;
            if (!serviceData.TryGetValue(serviceKey, out serviceBean))
            {
                xxlRpcResponse.errorMsg = "The serviceKey[" + serviceKey + "] not found.";
                return xxlRpcResponse;
            }

            if (DateTimeExtensions.CurrentTimeMillis() - xxlRpcRequest.createMillisTime > 3 * 60 * 1000)
            {
                xxlRpcResponse.errorMsg = "The timestamp difference between admin and executor exceeds the limit.";
                return xxlRpcResponse;
            }

            if (!string.IsNullOrEmpty(this.accessToken) && this.accessToken != xxlRpcRequest.accessToken)
            {
                xxlRpcResponse.errorMsg = "The access token[" + xxlRpcRequest.accessToken + "] is wrong.";
                return xxlRpcResponse;
            }

            try
            {
                var type = serviceBean.GetType();
                var methodName = xxlRpcRequest.methodName;
                var method = type.GetMethod(methodName);
                var result = method.Invoke(serviceBean, xxlRpcRequest.parameters);

                xxlRpcResponse.result = result;
            }
            catch (Exception ex)
            {
                xxlRpcResponse.errorMsg = ex.ToString();
            }

            return xxlRpcResponse;
        }

        internal void Stop()
        {
            throw new NotImplementedException();
        }

        internal void Start()
        {
            throw new NotImplementedException();
        }
    }
}
