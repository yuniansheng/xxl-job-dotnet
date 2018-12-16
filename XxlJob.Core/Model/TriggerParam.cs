using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xxl.job.core.biz.model
{
    public class TriggerParam
    {
        public int jobId;

        public string executorHandler;

        public string executorParams;

        public string executorBlockStrategy;

        public int executorTimeout;

        public int logId;

        public long logDateTim;

        public string glueType;

        public string glueSource;

        public long glueUpdatetime;

        public int broadcastIndex;

        public int broadcastTotal;
    }
}
