using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core
{
    public class JobExecutionContext
    {
        /// <summary>
        /// Job参数
        /// </summary>
        public string ExecutorParams { get; set; }

        /// <summary>
        /// 分片索引，0开始
        /// </summary>
        public int BroadcastIndex { get; set; }

        /// <summary>
        /// 分片总数
        /// </summary>
        public int BroadcastTotal { get; set; }
    }
}
