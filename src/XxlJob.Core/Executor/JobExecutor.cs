using com.xxl.job.core.biz.model;
using com.xxl.job.core.rpc.codec;
using hessiancsharp.io;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XxlJob.Core.RPC;
using XxlJob.Core.Threads;
using XxlJob.Core.Util;

namespace XxlJob.Core.Executor
{
    public class JobExecutor
    {
        private readonly IOptions<JobExecutorOption> _executorOption;
        private readonly JobThreadFactory _jobThreadFactory;
        private readonly ISerializer _serializer;

        public JobExecutor(IOptions<JobExecutorOption> executorOption, JobThreadFactory threadFactory, ILoggerFactory loggerFactory, ISerializer serializer)
        {
            _executorOption = executorOption;
            JobLogger.Init(_executorOption, loggerFactory);
            _jobThreadFactory = threadFactory;
            _serializer = serializer;
        }

        public byte[] HandleRequest(Stream inputStream)
        {
            var rpcRequest = _serializer.Deserialize(inputStream) as RpcRequest;
            var rpcResponse = rpcRequest.CreateRpcResponse();
            if (rpcRequest == null)
            {
                rpcResponse.Error = "The request is not valid.";
            }
            else
            {
                InvokeService(rpcRequest, rpcResponse);
            }

            return _serializer.Serialize(rpcResponse);
        }

        private void InvokeService(RpcRequest rpcRequest, RpcResponse rpcResponse)
        {
            if (rpcRequest.className != "com.xxl.job.core.biz.ExecutorBiz")
            {
                rpcResponse.Error = "The request is not a xxl-job request.";
                return;
            }

            if (DateTime.UtcNow.Subtract(DateTimeExtensions.FromMillis(rpcRequest.createMillisTime)) > Constants.RpcRequestExpireTimeSpan)
            {
                rpcResponse.Error = "The timestamp difference between admin and executor exceeds the limit.";
                return;
            }

            if (!string.IsNullOrEmpty(_executorOption.Value.AccessToken) && _executorOption.Value.AccessToken != rpcRequest.accessToken)
            {
                rpcResponse.Error = "The access token[" + rpcRequest.accessToken + "] is wrong.";
                return;
            }

            try
            {
                var type = this.GetType();
                var method = type.GetMethod(rpcRequest.methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (method == null)
                {
                    rpcResponse.Error = "The method[" + rpcRequest.methodName + "] not found.";
                    return;
                }
                var result = method.Invoke(this, rpcRequest.parameters.ToArray());
                rpcResponse.result = result;
            }
            catch (Exception ex)
            {
                rpcResponse.Error = ex.ToString();
            }
        }

        public void Start()
        {
        }


        private ReturnT Beat()
        {
            return ReturnT.SUCCESS;
        }

        private ReturnT IdleBeat(int jobId)
        {
            JobThread jobThread = _jobThreadFactory.FindJobThread(jobId);
            if (jobThread != null && jobThread.IsRunningOrHasQueue())
            {
                return ReturnT.CreateFailedResult("job thread is running or has trigger queue.");
            }
            return ReturnT.SUCCESS;
        }

        private ReturnT Kill(int jobId)
        {
            if (_jobThreadFactory.TryRemoveJobThread(jobId, "scheduling center kill job."))
            {
                return ReturnT.SUCCESS;
            }
            else
            {
                return ReturnT.CreateSucceededResult("job thread aleady killed.");
            }
        }

        private ReturnT Log(long logDateTime, int logId, int fromLineNum)
        {
            var logResult = JobLogger.ReadLog(logDateTime, logId, fromLineNum);
            return ReturnT.CreateSucceededResult(null, logResult);
        }

        private ReturnT Run(TriggerParam triggerParam)
        {
            if (Constants.GlueType.BEAN != triggerParam.glueType)
            {
                return ReturnT.CreateFailedResult("glueType[" + triggerParam.glueType + "] is not valid.");
            }

            JobThread jobThread;
            var isNewThread = _jobThreadFactory.GetJobThread(triggerParam, out jobThread);
            if (!isNewThread && Constants.ExecutorBlockStrategy.DISCARD_LATER == triggerParam.executorBlockStrategy && jobThread.IsRunningOrHasQueue())
            {
                return ReturnT.CreateFailedResult("block strategy effect：" + triggerParam.executorBlockStrategy);
            }

            var result = jobThread.PushTriggerQueue(triggerParam);
            if (isNewThread)
            {
                jobThread.Start();
            }
            return result;
        }
    }
}
