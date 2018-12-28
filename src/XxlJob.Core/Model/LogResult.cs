using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.xxl.job.core.biz.model
{
    public class LogResult
    {
        private int fromLineNum;
        private int toLineNum;
        private string logContent;
        private bool isEnd;

        public LogResult(int fromLineNum, int toLineNum, string logContent, bool isEnd)
        {
            this.fromLineNum = fromLineNum;
            this.toLineNum = toLineNum;
            this.logContent = logContent;
            this.isEnd = isEnd;
        }
    }
}
