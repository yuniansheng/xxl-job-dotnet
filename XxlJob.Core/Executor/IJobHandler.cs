using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    public abstract class IJobHandler
    {
        public static readonly ReturnT<string> SUCCESS = new ReturnT<string>(200, null);

        public static readonly ReturnT<string> FAIL = new ReturnT<string>(500, null);

        public static readonly ReturnT<string> FAIL_TIMEOUT = new ReturnT<string>(502, null);



        public abstract ReturnT<string> Execute(string param);

        public virtual void Init() { }

        public virtual void Destroy() { }
    }
}
