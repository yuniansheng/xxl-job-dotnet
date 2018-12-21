using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core
{
    internal static class Constants
    {
        public const int MAX_CALLBACK_RETRY_TIMES = 10;

        public static TimeSpan CallbackRetryInterval = TimeSpan.FromSeconds(10);

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
