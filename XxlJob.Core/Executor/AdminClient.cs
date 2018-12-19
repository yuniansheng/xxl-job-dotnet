using com.xxl.job.core.biz.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    internal class AdminClient
    {
        private readonly JobExecutorConfig _jobExecutorConfig;

        public AdminClient(JobExecutorConfig jobExecutorConfig)
        {
            _jobExecutorConfig = jobExecutorConfig;
        }

        public ReturnT Callback(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            //todo: send request
            return ReturnT.CreateFailedResult("not implemented yet");
        }
    }
}
