using com.xxl.job.core.biz.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core
{
    public abstract class IJobHandler
    {
        public abstract ReturnT Execute(JobExecutionContext context);
    }
}
