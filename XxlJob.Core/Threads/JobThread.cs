using com.xxl.job.core.biz.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XxlJob.Core.Threads
{
    internal class JobThread
    {
        private readonly int _jobId;
        private readonly JobExecutorConfig _executorConfig;
        private readonly ConcurrentQueue<TriggerParam> _triggerQueue;
        // avoid repeat trigger for the same TRIGGER_LOG_ID
        private readonly ConcurrentDictionary<int, byte> _triggerLogIdSet;
        private readonly AutoResetEvent _queueHasDataEvent;

        private Thread _thread;
        private bool _running = false;
        private volatile bool _toStop = false;
        private string _stopReason;

        public event EventHandler<HandleCallbackParam> OnCallback;

        public bool Stopped { get { return _toStop; } }

        public JobThread(int jobId, JobExecutorConfig executorConfig)
        {
            _jobId = jobId;
            _executorConfig = executorConfig;
            _triggerQueue = new ConcurrentQueue<TriggerParam>();
            _triggerLogIdSet = new ConcurrentDictionary<int, byte>();
            _queueHasDataEvent = new AutoResetEvent(false);
        }

        public ReturnT PushTriggerQueue(TriggerParam triggerParam)
        {
            // avoid repeat
            if (_triggerLogIdSet.ContainsKey(triggerParam.logId))
            {
                //logger.info(">>>>>>>>>>> repeate trigger job, logId:{}", triggerParam.getLogId());
                return ReturnT.CreateFailedResult("repeate trigger job, logId:" + triggerParam.logId);
            }

            _triggerLogIdSet[triggerParam.jobId] = 0;
            _triggerQueue.Enqueue(triggerParam);
            _queueHasDataEvent.Set();
            return ReturnT.SUCCESS;
        }

        public void Start()
        {
            _thread = new Thread(Run);
            _thread.Start();
        }

        public void ToStop(string stopReason)
        {
            /**
             * Thread.interrupt只支持终止线程的阻塞状态(wait、join、sleep)，
             * 在阻塞出抛出InterruptedException异常,但是并不会终止运行的线程本身；
             * 所以需要注意，此处彻底销毁本线程，需要通过共享变量方式；
             */
            _toStop = true;
            _stopReason = stopReason;
        }

        public void Interrupt(string stopReason)
        {
            ToStop(stopReason);
            _thread?.Interrupt();
        }

        public bool IsRunningOrHasQueue()
        {
            return _running || _triggerQueue.Count > 0;
        }



        private void Run()
        {
            TriggerParam triggerParam = null;
            while (!_toStop)
            {
                _running = false;
                triggerParam = null;
                ReturnT executeResult = null;
                try
                {
                    if (_triggerQueue.TryDequeue(out triggerParam))
                    {
                        _running = true;
                        byte temp;
                        _triggerLogIdSet.TryRemove(triggerParam.logId, out temp);
                        JobLogger.SetLogFileName(_executorConfig.LogPath, triggerParam.logDateTim, triggerParam.logId);
                        var executionContext = new JobExecutionContext()
                        {
                            BroadcastIndex = triggerParam.broadcastIndex,
                            BroadcastTotal = triggerParam.broadcastTotal,
                            ExecutorParams = triggerParam.executorParams
                        };
                        // execute
                        JobLogger.Log("<br>----------- xxl-job job execute start -----------<br>----------- Param:" + triggerParam.executorParams);

                        var handler = _executorConfig.JobHandlerFactory.GetJobHandler(triggerParam.executorHandler);
                        executeResult = handler.Execute(executionContext);

                        if (executeResult == null)
                        {
                            executeResult = ReturnT.FAIL;
                        }
                        JobLogger.Log("<br>----------- xxl-job job execute end(finish) -----------<br>----------- ReturnT:" + executeResult);
                    }
                    else
                    {
                        try
                        {
                            if (!_queueHasDataEvent.WaitOne(TimeSpan.FromSeconds(90)))
                            {
                                ToStop("excutor idel times over limit.");
                                break;
                            }
                        }
                        catch (ThreadInterruptedException)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.ToString();
                    executeResult = ReturnT.CreateFailedResult(errorMsg);
                    JobLogger.Log("<br>----------- JobThread Exception:" + errorMsg + "<br>----------- xxl-job job execute end(error) -----------");
                }
                finally
                {
                    if (executeResult != null)
                    {
                        OnCallback?.Invoke(this, new HandleCallbackParam(triggerParam.logId, triggerParam.logDateTim, executeResult));
                    }
                }
            }

            JobLogger.Log("<br>----------- JobThread toStop, stopReason:" + _stopReason);

            // callback trigger request in queue
            while (_triggerQueue.TryDequeue(out triggerParam))
            {
                var stopResult = ReturnT.CreateFailedResult(_stopReason + " [job not executed, in the job queue, killed.]");
                OnCallback?.Invoke(this, new HandleCallbackParam(triggerParam.logId, triggerParam.logDateTim, stopResult));
            }
        }
    }
}
