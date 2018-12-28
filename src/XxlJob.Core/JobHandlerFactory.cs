using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core
{
    public abstract class JobHandlerFactory
    {
        public abstract IJobHandler GetJobHandler(string handlerName);
    }    
}
