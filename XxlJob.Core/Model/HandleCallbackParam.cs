using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XxlJob.Core.Util;

namespace com.xxl.job.core.biz.model
{
    public class HandleCallbackParam
    {
        public HandleCallbackParam() { }

        public HandleCallbackParam(int logId, long logDateTim, ReturnT executeResult)
        {
            this.logId = logId;
            this.logDateTim = logDateTim;
            this.executeResult = executeResult;
        }

        public int logId;

        public long logDateTim;

        public ReturnT executeResult;
    }
}
