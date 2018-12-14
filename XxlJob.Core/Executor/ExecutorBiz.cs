using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    internal class ExecutorBiz
    {
        public ReturnT<string> beat()
        {
            return ReturnT.SUCCESS;
        }

        public ReturnT<string> idleBeat(int jobId)
        {
            // isRunningOrHasQueue
            bool isRunningOrHasQueue = false;
            JobThread jobThread = XxlJobExecutor.LoadJobThread(jobId);
            if (jobThread != null && jobThread.IsRunningOrHasQueue())
            {
                isRunningOrHasQueue = true;
            }

            if (isRunningOrHasQueue)
            {
                return new ReturnT<string>(ReturnT.FAIL_CODE, "job thread is running or has trigger queue.");
            }
            return ReturnT.SUCCESS;
        }

        public ReturnT<string> kill(int jobId)
        {
            // kill handlerThread, and create new one
            JobThread jobThread = XxlJobExecutor.LoadJobThread(jobId);
            if (jobThread != null)
            {
                XxlJobExecutor.RemoveJobThread(jobId, "scheduling center kill job.");
                return ReturnT.SUCCESS;
            }

            return new ReturnT<string>(ReturnT.SUCCESS_CODE, "job thread aleady killed.");
        }

        public ReturnT<string> run(TriggerParam triggerParam)
        {
            // load old：jobHandler + jobThread
            JobThread jobThread = XxlJobExecutor.LoadJobThread(triggerParam.JobId);
            IJobHandler jobHandler = jobThread?.Handler;
            string removeOldReason = null;

            // valid：jobHandler + jobThread            
            if (Constants.GlueType.BEAN == triggerParam.GlueType)
            {
                // new jobhandler
                IJobHandler newJobHandler = XxlJobExecutor.LoadJobHandler(triggerParam.ExecutorHandler);

                // valid old jobThread
                if (jobThread != null && jobHandler != newJobHandler)
                {
                    // change handler, need kill old thread
                    removeOldReason = "change jobhandler or glue type, and terminate the old job thread.";

                    jobThread = null;
                    jobHandler = null;
                }

                // valid handler
                if (jobHandler == null)
                {
                    jobHandler = newJobHandler;
                    if (jobHandler == null)
                    {
                        return new ReturnT<string>(ReturnT.FAIL_CODE, "job handler [" + triggerParam.ExecutorHandler + "] not found.");
                    }
                }
            }
            else
            {
                return new ReturnT<string>(ReturnT.FAIL_CODE, "glueType[" + triggerParam.GlueType + "] is not valid.");
            }

            // executor block strategy
            if (jobThread != null)
            {
                var blockStrategy = triggerParam.ExecutorBlockStrategy;
                if (Constants.ExecutorBlockStrategy.DISCARD_LATER == blockStrategy)
                {
                    // discard when running
                    if (jobThread.IsRunningOrHasQueue())
                    {
                        return new ReturnT<string>(ReturnT.FAIL_CODE, "block strategy effect：" + blockStrategy);
                    }
                }
                else if (Constants.ExecutorBlockStrategy.COVER_EARLY == blockStrategy)
                {
                    // kill running jobThread
                    if (jobThread.IsRunningOrHasQueue())
                    {
                        removeOldReason = "block strategy effect：" + blockStrategy;

                        jobThread = null;
                    }
                }
                else
                {
                    // just queue trigger
                }
            }

            // replace thread (new or exists invalid)
            if (jobThread == null)
            {
                jobThread = XxlJobExecutor.RegistJobThread(triggerParam.JobId, jobHandler, removeOldReason);
            }

            // push data to queue
            ReturnT<string> pushResult = jobThread.PushTriggerQueue(triggerParam);
            return pushResult;
        }
    }
}
