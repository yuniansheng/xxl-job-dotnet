using com.xxl.job.core.biz.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    public class JobThread
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

                        // log filename, like "logPath/yyyy-MM-dd/9999.log"
                        //stopReason logFileName = XxlJobFileAppender.makeLogFileName(new Date(triggerParam.getLogDateTim()), triggerParam.getLogId());
                        //XxlJobFileAppender.contextHolder.set(logFileName);
                        //ShardingUtil.setShardingVo(new ShardingUtil.ShardingVO(triggerParam.getBroadcastIndex(), triggerParam.getBroadcastTotal()));

                        // execute
                        //XxlJobLogger.log("<br>----------- xxl-job job execute start -----------<br>----------- Param:" + triggerParam.getExecutorParams());

                        var handler = _executorConfig.JobHandlerFactory.GetJobHandler(triggerParam.executorHandler);
                        if (triggerParam.executorTimeout > 0)
                        {
                            executeResult = handler.Execute(triggerParam.executorParams);
                        }
                        else
                        {
                            executeResult = handler.Execute(triggerParam.executorParams);
                        }

                        if (executeResult == null)
                        {
                            executeResult = IJobHandler.FAIL;
                        }
                        //XxlJobLogger.log("<br>----------- xxl-job job execute end(finish) -----------<br>----------- ReturnT:" + executeResult);
                    }
                    else
                    {
                        try
                        {
                            if (!_queueHasDataEvent.WaitOne(TimeSpan.FromSeconds(90)))
                            {
                                ToStop("excutor idel times over limit.");
                                //XxlJobExecutor.RemoveJobThread(_jobId, "excutor idel times over limit.");
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
                    //XxlJobLogger.log("<br>----------- JobThread Exception:" + errorMsg + "<br>----------- xxl-job job execute end(error) -----------");
                }
                finally
                {
                    if (executeResult != null)
                    {
                        //TriggerCallbackThread.pushCallBack(new HandleCallbackParam(triggerParam.getLogId(), triggerParam.getLogDateTim(), executeResult));
                    }
                }
            }

            //XxlJobLogger.log("<br>----------- JobThread toStop, stopReason:" + stopReason);            

            // callback trigger request in queue
            while (_triggerQueue.TryDequeue(out triggerParam))
            {
                var stopResult = ReturnT.CreateFailedResult(_stopReason + " [job not executed, in the job queue, killed.]");
                //TriggerCallbackThread.pushCallBack(new HandleCallbackParam(triggerParam.getLogId(), triggerParam.getLogDateTim(), stopResult));
            }

            //logger.info(">>>>>>>>>>> xxl-job JobThread stoped, hashCode:{}", Thread.currentThread());
        }
    }
}
