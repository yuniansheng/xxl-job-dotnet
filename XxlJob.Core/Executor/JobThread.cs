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
        private int jobId;
        private Thread thread;
        private ConcurrentQueue<TriggerParam> triggerQueue;
        // avoid repeat trigger for the same TRIGGER_LOG_ID
        private ConcurrentDictionary<int, byte> triggerLogIdSet;

        private volatile bool toStop = false;
        private string stopReason;
        private bool running = false;
        private int idleTimes = 0;

        public IJobHandler Handler { get; private set; }

        public JobThread(int jobId, IJobHandler handler)
        {
            this.jobId = jobId;
            this.Handler = handler;
            this.triggerQueue = new ConcurrentQueue<TriggerParam>();
            this.triggerLogIdSet = new ConcurrentDictionary<int, byte>();
        }

        public ReturnT<string> PushTriggerQueue(TriggerParam triggerParam)
        {
            // avoid repeat
            if (triggerLogIdSet.ContainsKey(triggerParam.LogId))
            {
                //logger.info(">>>>>>>>>>> repeate trigger job, logId:{}", triggerParam.getLogId());
                return new ReturnT<string>(ReturnT.FAIL_CODE, "repeate trigger job, logId:" + triggerParam.LogId);
            }

            triggerLogIdSet[triggerParam.JobId] = 0;
            triggerQueue.Enqueue(triggerParam);
            return ReturnT.SUCCESS;
        }

        public void ToStop(string stopReason)
        {
            /**
             * Thread.interrupt只支持终止线程的阻塞状态(wait、join、sleep)，
             * 在阻塞出抛出InterruptedException异常,但是并不会终止运行的线程本身；
             * 所以需要注意，此处彻底销毁本线程，需要通过共享变量方式；
             */
            this.toStop = true;
            this.stopReason = stopReason;
        }

        public bool IsRunningOrHasQueue()
        {
            return running || triggerQueue.Count > 0;
        }

        public void Start()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        private void Run()
        {
            // init
            try
            {
                Handler.Init();
            }
            catch (Exception ex)
            {
                //logger.error(e.getMessage(), e);
            }

            byte temp;
            TriggerParam triggerParam = null;

            // execute
            while (!toStop)
            {
                running = false;
                idleTimes++;

                ReturnT<string> executeResult = null;
                try
                {
                    if (triggerQueue.TryDequeue(out triggerParam))
                    {
                        running = true;
                        idleTimes = 0;
                        triggerLogIdSet.TryRemove(triggerParam.LogId, out temp);

                        // log filename, like "logPath/yyyy-MM-dd/9999.log"
                        //stopReason logFileName = XxlJobFileAppender.makeLogFileName(new Date(triggerParam.getLogDateTim()), triggerParam.getLogId());
                        //XxlJobFileAppender.contextHolder.set(logFileName);
                        //ShardingUtil.setShardingVo(new ShardingUtil.ShardingVO(triggerParam.getBroadcastIndex(), triggerParam.getBroadcastTotal()));

                        // execute
                        //XxlJobLogger.log("<br>----------- xxl-job job execute start -----------<br>----------- Param:" + triggerParam.getExecutorParams());

                        if (triggerParam.ExecutorTimeout > 0)
                        {
                            //executeResult = Handler.Execute(triggerParam.ExecutorParams);
                        }
                        else
                        {
                            // just execute
                            executeResult = Handler.Execute(triggerParam.ExecutorParams);
                        }

                        if (executeResult == null)
                        {
                            executeResult = IJobHandler.FAIL;
                        }
                        //XxlJobLogger.log("<br>----------- xxl-job job execute end(finish) -----------<br>----------- ReturnT:" + executeResult);
                    }
                    else
                    {
                        running = false;
                        idleTimes++;
                        if (idleTimes > 30)
                        {
                            XxlJobExecutor.RemoveJobThread(jobId, "excutor idel times over limit.");
                        }
                        //todo:sleep 3 seconds
                    }
                }
                catch (Exception ex)
                {
                    if (toStop)
                    {
                        //XxlJobLogger.log("<br>----------- JobThread toStop, stopReason:" + stopReason);
                    }

                    var errorMsg = ex.ToString();
                    executeResult = new ReturnT<String>(ReturnT.FAIL_CODE, errorMsg);

                    //XxlJobLogger.log("<br>----------- JobThread Exception:" + errorMsg + "<br>----------- xxl-job job execute end(error) -----------");
                }
                finally
                {
                    if (triggerParam != null)
                    {
                        // callback handler info
                        if (!toStop)
                        {
                            // commonm
                            //TriggerCallbackThread.pushCallBack(new HandleCallbackParam(triggerParam.getLogId(), triggerParam.getLogDateTim(), executeResult));
                        }
                        else
                        {
                            // is killed
                            ReturnT<string> stopResult = new ReturnT<string>(ReturnT.FAIL_CODE, stopReason + " [job running，killed]");
                            //TriggerCallbackThread.pushCallBack(new HandleCallbackParam(triggerParam.getLogId(), triggerParam.getLogDateTim(), stopResult));
                        }
                    }
                }
            }

            // callback trigger request in queue
            while (triggerQueue.TryDequeue(out triggerParam))
            {
                // is killed
                ReturnT<string> stopResult = new ReturnT<string>(ReturnT.FAIL_CODE, stopReason + " [job not executed, in the job queue, killed.]");
                //TriggerCallbackThread.pushCallBack(new HandleCallbackParam(triggerParam.getLogId(), triggerParam.getLogDateTim(), stopResult));
            }

            // destroy
            try
            {
                Handler.Destroy();
            }
            catch (Exception ex)
            {
                //logger.error(e.getMessage(), e);
            }

            //logger.info(">>>>>>>>>>> xxl-job JobThread stoped, hashCode:{}", Thread.currentThread());
        }

        public void Interrupt()
        {
            thread?.Interrupt();
        }
    }
}
