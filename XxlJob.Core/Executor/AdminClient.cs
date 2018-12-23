using com.xxl.job.core.biz.model;
using com.xxl.job.core.rpc.codec;
using hessiancsharp.io;
using java.lang;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    internal class AdminClient
    {
        private readonly JobExecutorConfig _jobExecutorConfig;
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private List<AddressEntry> _addresses;
        private int _currentAdminIndex;

        public bool IsAdminAccessable
        {
            get
            {
                return _addresses.Any();
            }
        }

        public AdminClient(IOptions<JobExecutorConfig> jobExecutorConfig, ILoggerFactory loggerFactory)
        {
            _jobExecutorConfig = jobExecutorConfig.Value;
            _client = new HttpClient();
            _client.Timeout = Constants.AdminServerDefaultTimeout;
            _logger = loggerFactory.CreateLogger<AdminClient>();
            InitAddress();
        }

        public ReturnT Callback(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            var paramTypes = new List<string>() { "java.util.List" };
            return InvokeService(MethodBase.GetCurrentMethod(), paramTypes, callbackParamList).Result;
        }

        public async Task<ReturnT> InvokeService(MethodBase method, List<string> paramTypes, params object[] parameters)
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

            var ms = new MemoryStream();
            var serializer = new CHessianOutput(ms);
            serializer.WriteObject(request);
            var responseBody = ms.ToArray();

            int triedTimes = 0;
            while (triedTimes++ < _addresses.Count)
            {
                var item = _addresses[_currentAdminIndex];
                _currentAdminIndex = (_currentAdminIndex + 1) % _addresses.Count;
                if (!item.CheckAccessable())
                    continue;

                Stream responseStream;
                try
                {
                    var content = new ByteArrayContent(responseBody);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    var responseMessage = await _client.PostAsync(item.RequestUri, content);
                    responseMessage.EnsureSuccessStatusCode();
                    responseStream = await responseMessage.Content.ReadAsStreamAsync();
                    item.Reset();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "request admin error.");
                    item.SetFail();
                    continue;
                }

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

            throw new Exception("xxl-rpc server address not accessable.");
        }

        private void InitAddress()
        {
            _addresses = new List<AddressEntry>();
            foreach (var item in _jobExecutorConfig.AdminAddresses)
            {
                try
                {
                    var uri = new Uri(item + "/api");
                    var entry = new AddressEntry { RequestUri = uri };
                    _addresses.Add(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "init admin address error.");
                }
            }
        }
    }

    internal class AddressEntry
    {
        public Uri RequestUri { get; set; }

        public DateTime? LastFailedTime { get; private set; }

        public int FailedTimes { get; private set; }

        public bool CheckAccessable()
        {
            if (LastFailedTime == null)
                return true;

            if (DateTime.UtcNow.Subtract(LastFailedTime.Value) > Constants.AdminServerReconnectInterval)
                return true;

            if (FailedTimes < Constants.AdminServerCircuitFaildTimes)
                return true;

            return false;
        }

        public void Reset()
        {
            LastFailedTime = null;
            FailedTimes = 0;
        }

        public void SetFail()
        {
            LastFailedTime = DateTime.UtcNow;
            FailedTimes++;
        }
    }
}
