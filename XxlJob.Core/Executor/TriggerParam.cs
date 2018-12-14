using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    public class TriggerParam
    {
        public int JobId { get; set; }

        public string ExecutorHandler { get; set; }

        public string ExecutorParams { get; set; }

        public string ExecutorBlockStrategy { get; set; }

        public int ExecutorTimeout { get; set; }

        public int LogId { get; set; }

        public long LogDateTim { get; set; }

        public string GlueType { get; set; }

        public string GlueSource { get; set; }

        public long GlueUpdatetime { get; set; }

        public int BroadcastIndex { get; set; }

        public int BroadcastTotal { get; set; }
    }
}
