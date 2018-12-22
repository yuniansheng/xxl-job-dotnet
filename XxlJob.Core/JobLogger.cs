using com.xxl.job.core.biz.model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core.Util;

namespace XxlJob.Core
{
    public static class JobLogger
    {
        private static JobExecutorConfig JobExecutorConfig;

        private static AsyncLocal<string> LogFileName = new AsyncLocal<string>();

        private static ILogger _logger;

        internal static void Init(JobExecutorConfig config)
        {
            JobExecutorConfig = config;
            _logger = config.LoggerFactory.CreateLogger("XxlJob.Core.JobLogger");
        }

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
        internal static void LogAtSpecifiedFile(long logDateTime, int logId, string content)
        {
            var filePath = MakeLogFileName(logDateTime, logId);
            var callInfo = new StackTrace(true).GetFrame(1);
            LogDetail(filePath, callInfo, content);
        }


        internal static LogResult ReadLog(long logDateTime, int logId, int fromLineNum)
        {
            var filePath = MakeLogFileName(logDateTime, logId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadLog error.");
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
                .Append("[line " + callInfo.GetFileLineNumber() + "]").Append("-")
                .Append("[thread " + Thread.CurrentThread.ManagedThreadId + "]").Append(" ")
                .Append(appendLog ?? string.Empty)
                .AppendLine();
            var formatAppendLog = stringBuffer.ToString();

            try
            {
                File.AppendAllText(logFileName, formatAppendLog, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogDetail error");
            }
        }

        internal static void SetLogFileName(long logDateTime, int logId)
        {
            try
            {
                var filePath = MakeLogFileName(logDateTime, logId);
                var dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    CleanOldLogs();
                }
                LogFileName.Value = filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetLogFileName error.");
            }
        }

        private static string GetLogFileName()
        {
            return LogFileName.Value;
        }

        private static string MakeLogFileName(long logDateTime, int logId)
        {
            //log fileName like: logPath/HandlerLogs/yyyy-MM-dd/9999.log
            return Path.Combine(JobExecutorConfig.LogPath, Constants.HandleLogsDirectory,
                DateTimeExtensions.FromMillis(logDateTime).ToString("yyyy-MM-dd"), $"{logId}.log");
        }

        private static void CleanOldLogs()
        {
            if (JobExecutorConfig.LogRetentionDays <= 0)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var handlerLogsDir = new DirectoryInfo(Path.Combine(JobExecutorConfig.LogPath, Constants.HandleLogsDirectory));
                    if (!handlerLogsDir.Exists)
                    {
                        return;
                    }

                    var today = DateTime.UtcNow.Date;
                    foreach (var dir in handlerLogsDir.GetDirectories())
                    {
                        DateTime dirDate;
                        if (DateTime.TryParse(dir.Name, out dirDate))
                        {
                            if (today.Subtract(dirDate.Date).Days > JobExecutorConfig.LogRetentionDays)
                            {
                                dir.Delete(true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CleanOldLogs error.");
                }
            });
        }
    }
}
