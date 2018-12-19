using com.xxl.job.core.biz.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    internal class TriggerCallbackThread
    {
        private readonly ConcurrentQueue<HandleCallbackParam> _callBackQueue = new ConcurrentQueue<HandleCallbackParam>();
        private readonly JobExecutorConfig _executorConfig;
        private readonly AutoResetEvent _queueHasDataEvent;
        private volatile bool toStop = false;
        private Thread callbackThread;
        private Thread retryThread;
        private string _callbackSavePath;
        private AdminClient _adminClient;

        public TriggerCallbackThread(JobExecutorConfig executorConfig)
        {
            _executorConfig = executorConfig;
            _queueHasDataEvent = new AutoResetEvent(false);
        }

        public void Start()
        {
            if (!_executorConfig.AdminAddresses.Any())
            {
                //logger.warn(">>>>>>>>>>> xxl-job, executor callback config fail, adminAddresses is null.");
                toStop = true;
                return;
            }

            _callbackSavePath = Path.Combine(_executorConfig.LogPath, "xxl-job-callback.log");
            var dir = Path.GetDirectoryName(_callbackSavePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _adminClient = new AdminClient(_executorConfig);

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
                    //logger.error(e.getMessage(), ex);
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
                    //logger.error(e.getMessage(), ex);
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
                SaveCallbackParams(new List<HandleCallbackParam> { callback });
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

            //logger.info(">>>>>>>>>>> xxl-job, executor callback thread destory.");
        }

        /// <summary>
        /// 从队列获取回调参数并执行回调
        /// </summary>
        /// <returns>如果本次执行方法未取到回调参数返回false，否则返回true</returns>
        private bool ConsumeAndCallback()
        {
            var callbackParamList = new List<HandleCallbackParam>();
            HandleCallbackParam callbackParam;
            while (_callBackQueue.TryDequeue(out callbackParam))
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
                //todo:log error
                LogCallbackResult(callbackParamList, "<br>----------- xxl-job job callback error, errorMsg:" + ex.Message);
            }

            SaveCallbackParams(callbackParamList);
        }



        private void RetryMethod()
        {
            while (!toStop)
            {
                try
                {
                    DoRetry();
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
                catch (ThreadInterruptedException ex)
                {
                    //logger.warn(">>>>>>>>>>> xxl-job, executor retry callback thread interrupted, error msg:{}", e.getMessage());
                    break;
                }
                catch (Exception e)
                {
                    //logger.error(e.getMessage(), e);
                }
            }

            //logger.info(">>>>>>>>>>> xxl-job, executor retry callback thread destory.");
        }

        private void DoRetry()
        {
            if (!File.Exists(_callbackSavePath))
            {
                return;
            }

            // load and clear file
            var fileLines = File.ReadAllLines(_callbackSavePath);
            File.Delete(_callbackSavePath);

            // retry callback, 100 lines per page
            var failCallbackParamList = new List<HandleCallbackParam>();
            for (int i = 0; i < fileLines.Length; i++)
            {
                var item = DeserializeHandleCallbackParam(fileLines[i]);
                failCallbackParamList.Add(item);
                if (failCallbackParamList.Count == 100 || i == fileLines.Length - 1)
                {
                    DoCallback(failCallbackParamList);
                    failCallbackParamList.Clear();
                }
            }
        }



        private void LogCallbackResult(IEnumerable<HandleCallbackParam> callbackParamList, string logContent)
        {
            foreach (var param in callbackParamList)
            {
                JobLogger.LogAtSpecifiedFile(_executorConfig.LogPath, param.logDateTim, param.logId, logContent);
            }
        }

        private void SaveCallbackParams(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            if (!callbackParamList.Any())
            {
                return;
            }

            var builder = new StringBuilder();
            foreach (var item in callbackParamList)
            {
                try
                {
                    var line = SerializeHandleCallbackParam(item);
                    builder.AppendLine(line);
                }
                catch (Exception)
                {
                    //todo:log error
                }
            }
            if (builder.Length > 0)
            {
                try
                {
                    File.AppendAllText(_callbackSavePath, builder.ToString());
                }
                catch (Exception)
                {
                    //todo:log error
                }
            }
        }

        private string SerializeHandleCallbackParam(HandleCallbackParam param)
        {
            throw new NotImplementedException();
        }

        private HandleCallbackParam DeserializeHandleCallbackParam(string content)
        {
            throw new NotImplementedException();
        }
    }
}
