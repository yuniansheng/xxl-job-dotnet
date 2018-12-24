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
using XxlJob.Core.Threads;
using XxlJob.Core.Util;

namespace XxlJob.Core.Executor
{
    internal class JobExecutor
    {
        private readonly IOptions<JobExecutorOption> _executorOption;
        private readonly JobThreadFactory _jobThreadFactory;

        public JobExecutor(IOptions<JobExecutorOption> executorOption, JobThreadFactory threadFactory, ILoggerFactory loggerFactory)
        {
            _executorOption = executorOption;
            JobLogger.Init(_executorOption, loggerFactory);
            _jobThreadFactory = threadFactory;
        }

        public byte[] HandleRequest(Stream inputStream)
        {
            var hessianInput = new CHessianInput(inputStream);
            var rpcRequest = hessianInput.ReadObject() as RpcRequest;
            var rpcResponse = new RpcResponse();
            if (rpcRequest == null)
            {
                rpcResponse.error = "The request is not valid.";
            }
            else
            {
                rpcResponse.requestId = rpcRequest.requestId;
                InvokeService(rpcRequest, rpcResponse);
            }

            using (var outputStream = new MemoryStream())
            {
                new CHessianOutput(outputStream).WriteObject(rpcResponse);
                return outputStream.GetBuffer();
            }
        }

        private void InvokeService(RpcRequest rpcRequest, RpcResponse rpcResponse)
        {
            if (rpcRequest.className != "com.xxl.job.core.biz.ExecutorBiz")
            {
                rpcResponse.error = "The request is not a xxl-job request.";
                return;
            }

            if (DateTime.UtcNow.Subtract(DateTimeExtensions.FromMillis(rpcRequest.createMillisTime)) > Constants.RpcRequestExpireTimeSpan)
            {
                rpcResponse.error = "The timestamp difference between admin and executor exceeds the limit.";
                return;
            }

            if (!string.IsNullOrEmpty(_executorOption.Value.AccessToken) && _executorOption.Value.AccessToken != rpcRequest.accessToken)
            {
                rpcResponse.error = "The access token[" + rpcRequest.accessToken + "] is wrong.";
                return;
            }

            try
            {
                var type = this.GetType();
                var method = type.GetMethod(rpcRequest.methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                if (method == null)
                {
                    rpcResponse.error = "The method[" + rpcRequest.methodName + "] not found.";
                    return;
                }
                var result = method.Invoke(this, rpcRequest.parameters.ToArray());
                rpcResponse.result = result;
            }
            catch (Exception ex)
            {
                rpcResponse.error = ex.ToString();
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
