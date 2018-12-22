using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core
{
    internal static class Constants
    {
        public const string XxlLogsDefaultRootDirectory = "xxl-job-logs";
        public const string HandleLogsDirectory = "HandlerLogs";
        public const string LogFileNameCallContextKey = "XxlJob.LogFileName";
        public const int DefaultLogRetentionDays = 30;

        public const int MaxCallbackRetryTimes = 10;
        public static TimeSpan CallbackRetryInterval = TimeSpan.FromSeconds(600);
        //Admin集群中的某台机器熔断后间隔多长时间再重试
        public static TimeSpan AdminServerReconnectInterval = TimeSpan.FromMinutes(3);
        //Admin集群中的某台机器请求失败多少次后熔断
        public const int AdminServerCircuitFaildTimes = 3;

        public static TimeSpan JobThreadWaitTime = TimeSpan.FromSeconds(90);

        public static class GlueType
        {
            public const string BEAN = "BEAN";
        }

        public static class ExecutorBlockStrategy
        {
            public const string SERIAL_EXECUTION = "SERIAL_EXECUTION";

            public const string DISCARD_LATER = "DISCARD_LATER";

            public const string COVER_EARLY = "COVER_EARLY";
        }
    }
}
