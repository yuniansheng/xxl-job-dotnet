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
            AdminAddresses = new List<string>();
        }

        public string AccessToken { get; set; }

        public JobHandlerFactory JobHandlerFactory { get; set; }

        /// <summary>
        /// 执行日志保存目录，默认AppDomain.CurrentDomain.BaseDirectory
        /// </summary>
        public string LogPath { get; set; }

        /// <summary>
        /// 调度中心地址列表，多个用逗号分隔，例如 "http://address" or "http://address01,http://address02"
        /// </summary>
        public List<string> AdminAddresses { get; set; }
    }
}
