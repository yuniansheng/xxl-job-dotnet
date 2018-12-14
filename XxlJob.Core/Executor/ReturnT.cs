using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    public class ReturnT
    {
        public const int SUCCESS_CODE = 200;
        public const int FAIL_CODE = 500;

        public static readonly ReturnT<string> SUCCESS = new ReturnT<string>(null);
        public static readonly ReturnT<string> FAIL = new ReturnT<string>(FAIL_CODE, null);

        public int code;
        public string msg;

        public ReturnT() { }
    }

    public class ReturnT<T> : ReturnT
    {
        public T content;

        public ReturnT(int code, string msg)
        {
            this.code = code;
            this.msg = msg;
        }

        public ReturnT(T content)
        {
            this.code = SUCCESS_CODE;
            this.content = content;
        }

        public override string ToString()
        {
            return "ReturnT [code=" + code + ", msg=" + msg + ", content=" + content + "]";
        }
    }
}
