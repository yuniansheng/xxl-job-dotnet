using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xxl.job.core.biz.model
{
    public class ReturnT
    {
        public const int SUCCESS_CODE = 200;
        public const int FAIL_CODE = 500;

        public static readonly ReturnT SUCCESS = new ReturnT(SUCCESS_CODE, null);
        public static readonly ReturnT FAIL = new ReturnT(FAIL_CODE, null);
        public static readonly ReturnT FAIL_TIMEOUT = new ReturnT(502, null);

        public int code;
        public string msg;
        public object content;

        public ReturnT() { }

        public ReturnT(int code, string msg)
        {
            this.code = code;
            this.msg = msg;
        }

        public static ReturnT CreateSucceededResult(string msg)
        {
            return CreateSucceededResult(msg, null);
        }

        public static ReturnT CreateSucceededResult(string msg, object content)
        {
            return new ReturnT(SUCCESS_CODE, msg) { content = content };
        }

        public static ReturnT CreateFailedResult(string msg)
        {
            return new ReturnT(FAIL_CODE, msg);
        }

        public override string ToString()
        {
            return "ReturnT [code=" + code + ", msg=" + msg + ", content=" + content + "]";
        }
    }
}
