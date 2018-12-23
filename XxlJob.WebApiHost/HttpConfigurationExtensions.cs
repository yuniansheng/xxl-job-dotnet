using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using XxlJob.Core;
using XxlJob.Core.Executor;

namespace XxlJob.WebApiHost
{
    public static class HttpConfigurationExtensions
    {
        private static readonly string DefaultListenPath = string.Empty;


        public static void EnableXxlJob(this HttpConfiguration httpConfiguration, JobExecutor executor)
        {
            EnableXxlJob(httpConfiguration, executor, DefaultListenPath);
        }

        public static void EnableXxlJob(this HttpConfiguration httpConfiguration, JobExecutor executor, string listenPath)
        {
            httpConfiguration.Routes.MapHttpRoute(
                name: "xxl-job",
                routeTemplate: listenPath,
                defaults: null,
                constraints: new { isXxlJob = new XxlJobConstraint() },
                handler: new XxlJobExecutorHandler(executor)
            );
        }
    }
}
