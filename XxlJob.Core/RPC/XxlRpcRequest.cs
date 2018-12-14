using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.RPC
{
    public class XxlRpcRequest
    {
        public string requestId;
        public long createMillisTime;
        public string accessToken;

        public string className;
        public string methodName;
        //private Class<?>[] parameterTypes;
        public object[] parameters;

        public string version;
    }
}
