using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XxlJob.Core.Executor;

namespace XxlJob.Core
{
    public class JobExecutorConfig
    {
        public JobExecutorConfig()
        {
            JobHandlerFactory = new DefaultJobHandlerFactory();
            LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "xxl-job", "JobHandlerLogs");
        }

        public string AccessToken { get; set; }

        public JobHandlerFactory JobHandlerFactory { get; set; }

        public string LogPath { get; set; }
    }
}
