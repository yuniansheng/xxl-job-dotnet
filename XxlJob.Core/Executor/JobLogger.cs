using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    public static class JobLogger
    {
        public static void Log(string format, params object[] args)
        {
            var appendLog = string.Format(format, args);
            var callInfo = new StackTrace(true).GetFrame(1);
            LogDetail(callInfo, appendLog);
        }

        public static void Log(Exception ex)
        {
            var callInfo = new StackTrace(true).GetFrame(1);
            LogDetail(callInfo, ex.ToString());
        }

        private static void LogDetail(StackFrame callInfo, string appendLog)
        {
            var stringBuffer = new StringBuilder();
            stringBuffer
                .Append(DateTime.Now.ToString("s")).Append(" ")
                .Append("[" + callInfo.GetMethod().DeclaringType.FullName + "#" + callInfo.GetMethod().Name + "]").Append("-")
                .Append("[" + callInfo.GetFileLineNumber() + "]").Append("-")
                .Append("[" + Thread.CurrentThread.Name + "]").Append(" ")
                .Append(appendLog ?? string.Empty)
                .AppendLine();
            var formatAppendLog = stringBuffer.ToString();

            AppendLog(formatAppendLog);
        }

        private static void AppendLog(string appendLog)
        {
            var logFileName = GetLogFileName();
            if (string.IsNullOrEmpty(logFileName))
            {
                return;
            }

            try
            {
                File.AppendAllText(logFileName, appendLog);
            }
            catch (Exception)
            {
                //todo:log error
            }
        }

        private static void SetLogFileName(string filePath)
        {
            try
            {
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
    }
}
