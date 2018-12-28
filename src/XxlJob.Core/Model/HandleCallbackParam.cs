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

        /// <summary>
        /// 回调重试次数,限制重试使用的,调度中心并不需要此字段
        /// </summary>
        public int callbackRetryTimes;

        public int logId;

        public long logDateTim;

        public ReturnT executeResult;
    }
}
