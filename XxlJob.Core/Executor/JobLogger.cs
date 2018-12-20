using com.xxl.job.core.biz.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core.Util;

namespace XxlJob.Core.Executor
{
    public static class JobLogger
    {
        public static void Log(string format, params object[] args)
        {
            var appendLog = string.Format(format, args);
            var callInfo = new StackTrace(true).GetFrame(1);
            LogDetail(GetLogFileName(), callInfo, appendLog);
        }

        public static void Log(Exception ex)
        {
            var callInfo = new StackTrace(true).GetFrame(1);
            LogDetail(GetLogFileName(), callInfo, ex.ToString());
        }

        /// <summary>
        /// 在由logDateTime、logId指定的文件记录日志
        /// </summary>
        internal static void LogAtSpecifiedFile(string logPath, long logDateTime, int logId, string content)
        {
            var filePath = MakeLogFileName(logPath, logDateTime, logId);
            var callInfo = new StackTrace(true).GetFrame(1);
            LogDetail(filePath, callInfo, content);
        }


        internal static LogResult ReadLog(string logPath, long logDateTime, int logId, int fromLineNum)
        {
            var filePath = MakeLogFileName(logPath, logDateTime, logId);
            if (string.IsNullOrEmpty(filePath))
            {
                return new LogResult(fromLineNum, 0, "readLog fail, logFile not found", true);
            }
            if (!File.Exists(filePath))
            {
                return new LogResult(fromLineNum, 0, "readLog fail, logFile not exists", true);
            }

            // read file
            var logContentBuffer = new StringBuilder();
            int toLineNum = 0;
            try
            {
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        toLineNum++;
                        if (toLineNum >= fromLineNum)
                        {
                            logContentBuffer.AppendLine(line);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //todo:log error                
            }

            // result
            LogResult logResult = new LogResult(fromLineNum, toLineNum, logContentBuffer.ToString(), false);
            return logResult;
        }

        private static void LogDetail(string logFileName, StackFrame callInfo, string appendLog)
        {
            if (string.IsNullOrEmpty(logFileName))
            {
                return;
            }

            var stringBuffer = new StringBuilder();
            stringBuffer
                .Append(DateTime.Now.ToString("s")).Append(" ")
                .Append("[" + callInfo.GetMethod().DeclaringType.FullName + "#" + callInfo.GetMethod().Name + "]").Append("-")
                .Append("[" + callInfo.GetFileLineNumber() + "]").Append("-")
                .Append("[thread " + Thread.CurrentThread.ManagedThreadId + "]").Append(" ")
                .Append(appendLog ?? string.Empty)
                .AppendLine();
            var formatAppendLog = stringBuffer.ToString();

            try
            {
                File.AppendAllText(logFileName, formatAppendLog, Encoding.UTF8);
            }
            catch (Exception)
            {
                //todo:log error
            }
        }

        internal static void SetLogFileName(string logPath, long logDateTime, int logId)
        {
            try
            {
                var filePath = MakeLogFileName(logPath, logDateTime, logId);
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                CallContext.LogicalSetData("XxlJob.LogFileName", filePath);
            }
            catch (Exception)
            {
                //todo:log error
            }
        }

        private static string GetLogFileName()
        {
            try
            {
                return CallContext.LogicalGetData("XxlJob.LogFileName") as string;
            }
            catch (Exception)
            {
                //todo:log error
                return null;
            }
        }

        private static string MakeLogFileName(string logPath, long logDateTime, int logId)
        {
            //log fileName like: logPath/HandlerLogs/yyyy-MM-dd/9999.log
            return Path.Combine(logPath, "HandlerLogs", DateTimeExtensions.FromMillis(logDateTime).ToString("yyyy-MM-dd"), $"{logId}.log");
        }
    }
}
