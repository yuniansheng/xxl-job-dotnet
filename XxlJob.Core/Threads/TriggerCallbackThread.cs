using com.xxl.job.core.biz.model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core.Executor;

namespace XxlJob.Core.Threads
{
    internal class TriggerCallbackThread
    {
        private readonly ConcurrentQueue<HandleCallbackParam> _callBackQueue = new ConcurrentQueue<HandleCallbackParam>();
        private readonly JobExecutorOption _executorOption;
        private readonly AutoResetEvent _queueHasDataEvent;
        private readonly ILogger _logger;
        private volatile bool toStop = false;
        private Thread callbackThread;
        private Thread retryThread;

        private AdminClient _adminClient;
        private HandleCallbackParamRepository _paramRepository;

        public TriggerCallbackThread(IOptions<JobExecutorOption> executorOption, ILoggerFactory loggerFactory, AdminClient adminClient, HandleCallbackParamRepository paramRepository)
        {
            _executorOption = executorOption.Value;
            _queueHasDataEvent = new AutoResetEvent(false);
            _logger = loggerFactory.CreateLogger<TriggerCallbackThread>();
            _adminClient = adminClient;
            _paramRepository = paramRepository;
        }

        public void Start()
        {
            //_adminClient = new AdminClient(_executorConfig);
            if (!_adminClient.IsAdminAccessable)
            {
                _logger.LogWarning("xxl-job, executor callback config fail, adminAddresses is not accessable.");
                toStop = true;
                return;
            }
            //_paramRepository = new HandleCallbackParamRepository(_executorConfig);

            callbackThread = new Thread(CallbackMethod);
            retryThread = new Thread(RetryMethod);

            callbackThread.Start();
            retryThread.Start();
        }

        public void Stop()
        {
            toStop = true;

            if (callbackThread != null)
            {
                // stop callback, interrupt and wait
                callbackThread.Interrupt();
                try
                {
                    callbackThread.Join();
                }
                catch (ThreadInterruptedException ex)
                {
                    _logger.LogError(ex, "callbackThread interrupted.");
                }
            }

            if (retryThread != null)
            {
                // stop retry, interrupt and wait
                retryThread.Interrupt();
                try
                {
                    retryThread.Join();
                }
                catch (ThreadInterruptedException ex)
                {
                    _logger.LogError(ex, "retryThread interrupted.");
                }
            }
        }

        public void PushCallBackParam(HandleCallbackParam callback)
        {
            if (!toStop)
            {
                _callBackQueue.Enqueue(callback);
                _queueHasDataEvent.Set();
            }
            else
            {
                _paramRepository.SaveCallbackParams(new List<HandleCallbackParam> { callback });
            }
        }



        private void CallbackMethod()
        {
            try
            {
                // normal callback
                while (!toStop)
                {
                    if (!ConsumeAndCallback())
                    {
                        _queueHasDataEvent.WaitOne();
                    }
                }
            }
            catch (Exception)
            {
                // last callback
                ConsumeAndCallback();
            }

            _logger.LogInformation("xxl-job, executor callback thread destory.");
        }

        /// <summary>
        /// 从队列获取回调参数并执行回调
        /// </summary>
        /// <returns>如果本次执行方法未取到回调参数返回false，否则返回true</returns>
        private bool ConsumeAndCallback()
        {
            var callbackParamList = new List<HandleCallbackParam>();
            HandleCallbackParam callbackParam;
            while (callbackParamList.Count < Constants.MaxCallbackRecordsPerRequest && _callBackQueue.TryDequeue(out callbackParam))
            {
                callbackParamList.Add(callbackParam);
            }

            if (!callbackParamList.Any())
            {
                return false;
            }

            DoCallback(callbackParamList);
            return true;
        }

        private void DoCallback(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            try
            {
                var callbackResult = _adminClient.Callback(callbackParamList);
                if (ReturnT.SUCCESS_CODE == callbackResult.code)
                {
                    LogCallbackResult(callbackParamList, "<br>----------- xxl-job job callback finish.");
                    return;
                }
                else
                {
                    LogCallbackResult(callbackParamList, "<br>----------- xxl-job job callback fail, callbackResult:" + callbackResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "xxl-job, callback admin error.");
                LogCallbackResult(callbackParamList, "<br>----------- xxl-job job callback error, errorMsg:" + ex.Message);
            }

            _paramRepository.SaveCallbackParams(callbackParamList);
        }

        private void LogCallbackResult(IEnumerable<HandleCallbackParam> callbackParamList, string logContent)
        {
            foreach (var param in callbackParamList)
            {
                JobLogger.LogAtSpecifiedFile(param.logDateTim, param.logId, logContent);
            }
        }



        private void RetryMethod()
        {
            while (!toStop)
            {
                try
                {
                    DoRetry();
                    Thread.Sleep(Constants.CallbackRetryInterval);
                }
                catch (ThreadInterruptedException ex)
                {
                    _logger.LogWarning(ex, "xxl-job, executor retry callback thread interrupted.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "xxl-job, executor retry callback thread error.");
                }
            }

            _logger.LogInformation("xxl-job, executor retry callback thread destory.");
        }

        private void DoRetry()
        {
            // retry callback, 100 lines per page
            var failCallbackParamList = _paramRepository.LoadCallbackParams();
            int currentIndex = 0;
            while (currentIndex < failCallbackParamList.Count)
            {
                var count = Math.Min(Constants.MaxCallbackRecordsPerRequest, failCallbackParamList.Count - currentIndex);
                var page = failCallbackParamList.GetRange(currentIndex, count);
                DoCallback(failCallbackParamList);
                currentIndex += count;
            }
        }
    }
}
