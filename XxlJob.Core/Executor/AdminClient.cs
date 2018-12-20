using com.xxl.job.core.biz.model;
using com.xxl.job.core.rpc.codec;
using hessiancsharp.io;
using java.lang;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XxlJob.Core.Util;

namespace XxlJob.Core.Executor
{
    public class AdminClient
    {
        private readonly JobExecutorConfig _jobExecutorConfig;

        public AdminClient(JobExecutorConfig jobExecutorConfig)
        {
            _jobExecutorConfig = jobExecutorConfig;
        }

        public ReturnT Callback(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            var paramTypes = new List<string>() { "java.util.List" };
            return InvokeService(MethodBase.GetCurrentMethod(), paramTypes, callbackParamList);
        }

        public ReturnT InvokeService(MethodBase method, List<string> paramTypes, params object[] parameters)
        {
            var methodName = method.Name.Substring(0, 1).ToLower() + method.Name.Substring(1);
            var request = new RpcRequest()
            {
                createMillisTime = DateTimeExtensions.CurrentTimeMillis(),
                accessToken = _jobExecutorConfig.AccessToken,
                className = "com.xxl.job.core.biz.AdminBiz",
                methodName = methodName,
                parameterTypes = new ArrayList(paramTypes.Select(item => new Class(item)).ToArray()),
                parameters = new ArrayList(parameters)
            };

            using (var client = new HttpClient())
            {
                var ms = new MemoryStream();
                var serializer = new CHessianOutput(ms);
                serializer.WriteObject(request);

                var content = new ByteArrayContent(ms.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var postTask = client.PostAsync("http://172.18.21.144:8080/xxl-job-admin/api", content);
                var responseStream = postTask.Result.Content.ReadAsStreamAsync().Result;
                var rpcResponse = (RpcResponse)new CHessianInput(responseStream).ReadObject();

                if (rpcResponse == null)
                {
                    throw new Exception("xxl-rpc response not found.");
                }
                if (rpcResponse.IsError)
                {
                    throw new Exception(rpcResponse.error);
                }
                else
                {
                    return rpcResponse.result as ReturnT;
                }
            }
        }
    }
}
