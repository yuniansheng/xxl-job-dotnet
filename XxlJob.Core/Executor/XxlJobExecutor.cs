using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core.RPC.Remoting.Provider;

namespace XxlJob.Core.Executor
{
    public class XxlJobExecutor
    {
        public string AccessToken { get; set; }


        // ---------------------- start + stop ----------------------
        public void Start()
        {
            InitRpcProvider(AccessToken);
        }

        public void Destroy()
        {
            // destory jobThreadRepository
            foreach (var entry in jobThreadRepository)
            {
                RemoveJobThread(entry.Key, "web container destroy and kill the job.");
            }
            jobThreadRepository.Clear();

            StopRpcProvider();
        }


        // ---------------------- executor-server (rpc provider) ----------------------
        private XxlRpcProviderFactory xxlRpcProviderFactory = null;

        private void InitRpcProvider(string accessToken)
        {
            var xxlRpcProviderFactory = new XxlRpcProviderFactory();
            xxlRpcProviderFactory.InitConfig(accessToken);

            xxlRpcProviderFactory.AddService(typeof(ExecutorBiz).FullName, null, new ExecutorBiz());

            xxlRpcProviderFactory.Start();
        }

        private void StopRpcProvider()
        {
            // stop provider factory
            try
            {
                xxlRpcProviderFactory.Stop();
            }
            catch (Exception e)
            {
                //logger.error(e.getMessage(), e);
            }
        }


        #region job handler repository

        private static ConcurrentDictionary<string, IJobHandler> jobHandlerRepository = new ConcurrentDictionary<string, IJobHandler>();

        public static IJobHandler RegistJobHandler(string name, IJobHandler jobHandler)
        {
            //logger.info(">>>>>>>>>>> xxl-job register jobhandler success, name:{}, jobHandler:{}", name, jobHandler);
            jobHandlerRepository[name] = jobHandler;
            return jobHandler;
        }

        public static IJobHandler LoadJobHandler(string name)
        {
            return jobHandlerRepository[name];
        }

        #endregion




        #region job thread repository

        private static ConcurrentDictionary<int, JobThread> jobThreadRepository = new ConcurrentDictionary<int, JobThread>();

        public static JobThread RegistJobThread(int jobId, IJobHandler handler, string removeOldReason)
        {
            JobThread newJobThread = new JobThread(jobId, handler);
            newJobThread.Start();
            //logger.info(">>>>>>>>>>> xxl-job regist JobThread success, jobId:{}, handler:{}", new Object[] { jobId, handler });

            return jobThreadRepository.AddOrUpdate(jobId, newJobThread, (oldJobId, oldJobThread) =>
            {
                oldJobThread.ToStop(removeOldReason);
                oldJobThread.Interrupt();
                return newJobThread;
            });
        }

        public static void RemoveJobThread(int jobId, string removeOldReason)
        {
            JobThread oldJobThread;
            if (jobThreadRepository.TryRemove(jobId, out oldJobThread))
            {
                oldJobThread.ToStop(removeOldReason);
                oldJobThread.Interrupt();
            }
        }

        public static JobThread LoadJobThread(int jobId)
        {
            return jobThreadRepository[jobId];
        }

        #endregion
    }
}
