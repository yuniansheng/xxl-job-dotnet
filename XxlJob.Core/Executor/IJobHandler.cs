using com.xxl.job.core.biz.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    public abstract class IJobHandler
    {
        public static readonly ReturnT SUCCESS = new ReturnT(200, null);

        public static readonly ReturnT FAIL = new ReturnT(500, null);

        public static readonly ReturnT FAIL_TIMEOUT = new ReturnT(502, null);



        public abstract ReturnT Execute(string param);

        public virtual void Init() { }

        public virtual void Destroy() { }
    }
}
