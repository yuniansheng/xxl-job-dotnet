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
    public class JobThreadFactory
    {
        private readonly JobExecutorConfig _executorConfig;
        private readonly Dictionary<int, JobThread> _jobThreads = new Dictionary<int, JobThread>();
        private readonly Lazy<TriggerCallbackThread> _callbackThread;
        private readonly object _syncObject = new object();

        public JobThreadFactory(JobExecutorConfig executorConfig)
        {
            _executorConfig = executorConfig;
            _callbackThread = new Lazy<TriggerCallbackThread>(CreateAndStartCallbackThread, LazyThreadSafetyMode.ExecutionAndPublication);
        }


        public JobThread FindJobThread(int jobId)
        {
            lock (_syncObject)
            {
                JobThread jobThread;
                _jobThreads.TryGetValue(jobId, out jobThread);
                return jobThread;
            }
        }

        /// <summary>
        /// 获取JobThread
        /// </summary>
        /// <returns>true表示新创建的线程，否则表示原有的</returns>
        public bool GetJobThread(TriggerParam triggerParam, out JobThread jobThread)
        {
            lock (_syncObject)
            {
                if (_jobThreads.TryGetValue(triggerParam.jobId, out jobThread))
                {
                    if (jobThread.Stopped)
                    {
                        jobThread = CreateJobThread(triggerParam.jobId);
                        return true;
                    }

                    if (jobThread.IsRunningOrHasQueue() && Constants.ExecutorBlockStrategy.COVER_EARLY == triggerParam.executorBlockStrategy)
                    {
                        jobThread.Interrupt("block strategy effect：" + triggerParam.executorBlockStrategy);
                        jobThread = CreateJobThread(triggerParam.jobId);
                        return true;
                    }

                    return false;
                }
                else
                {
                    jobThread = CreateJobThread(triggerParam.jobId);
                    return true;
                }
            }
        }

        public bool TryRemoveJobThread(int jobId, string removeOldReason)
        {
            lock (_syncObject)
            {
                JobThread oldJobThread;
                if (_jobThreads.TryGetValue(jobId, out oldJobThread))
                {
                    oldJobThread.Interrupt(removeOldReason);
                    _jobThreads.Remove(jobId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private JobThread CreateJobThread(int jobId)
        {
            var jobThread = new JobThread(jobId, _executorConfig);
            jobThread.OnCallback += (sender, arg) =>
            {
                try
                {
                    _callbackThread.Value.PushCallBackParam(arg);
                }
                catch (Exception)
                {
                    //todo:log errro
                }
            };
            _jobThreads[jobId] = jobThread;
            return jobThread;
        }

        private TriggerCallbackThread CreateAndStartCallbackThread()
        {
            var thread = new TriggerCallbackThread(_executorConfig);
            thread.Start();
            return thread;
        }
    }
}
