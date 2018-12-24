using com.xxl.job.core.biz.model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core.Threads;

namespace XxlJob.Core.Executor
{
    public class JobThreadFactory
    {
        private readonly Dictionary<int, JobThread> _jobThreads = new Dictionary<int, JobThread>();
        private readonly Lazy<TriggerCallbackThread> _callbackThread;
        private readonly object _syncObject = new object();
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;

        public JobThreadFactory(IServiceProvider services, ILoggerFactory loggerFactory)
        {
            _services = services;
            _callbackThread = new Lazy<TriggerCallbackThread>(CreateAndStartCallbackThread, LazyThreadSafetyMode.ExecutionAndPublication);
            _logger = loggerFactory.CreateLogger<JobThreadFactory>();
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
            var jobThread = _services.GetService<JobThread>();
            jobThread.OnCallback += (sender, arg) =>
            {
                try
                {
                    _callbackThread.Value.PushCallBackParam(arg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "jobThread oncallback failed.");
                }
            };
            _jobThreads[jobId] = jobThread;
            return jobThread;
        }

        private TriggerCallbackThread CreateAndStartCallbackThread()
        {
            //var thread = new TriggerCallbackThread(_executorConfig);
            var thread = _services.GetService<TriggerCallbackThread>();
            thread.Start();
            return thread;
        }
    }
}
