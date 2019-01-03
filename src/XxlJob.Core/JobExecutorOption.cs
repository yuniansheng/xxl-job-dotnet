using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XxlJob.Core.Executor;
using XxlJob.Core.RPC;

namespace XxlJob.Core
{
    public class JobExecutorOption
    {
        public JobExecutorOption()
        {
            LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.XxlLogsDefaultRootDirectory);
            LogRetentionDays = Constants.DefaultLogRetentionDays;
            AdminAddresses = new List<string>();
            SerializeProtocol = SerializeProtocol.Hessian2;
        }

        public string AccessToken { get; set; }

        /// <summary>
        /// 执行日志保存目录，默认AppDomain.CurrentDomain.BaseDirectory
        /// </summary>
        public string LogPath { get; set; }

        /// <summary>
        /// 日志文件保留时间，默认保留30天，超过指定天数将被清除，如果指定0则永远不被清除
        /// </summary>
        public int LogRetentionDays { get; set; }

        /// <summary>
        /// 调度中心地址列表，多个用逗号分隔，例如 "http://address" or "http://address01,http://address02"
        /// </summary>
        public List<string> AdminAddresses { get; set; }

        /// <summary>
        /// 序列化协议，默认值为Hessian2
        /// </summary>
        public SerializeProtocol SerializeProtocol { get; set; }
    }
}
