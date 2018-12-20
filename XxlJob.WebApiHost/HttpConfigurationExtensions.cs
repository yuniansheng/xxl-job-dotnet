using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using XxlJob.Core;

namespace XxlJob.WebApiHost
{
    public static class HttpConfigurationExtensions
    {
        private static readonly string DefaultListenPath = string.Empty;


        public static void EnableXxlJob(this HttpConfiguration httpConfiguration, Action<JobExecutorConfig> configure = null)
        {
            EnableXxlJob(httpConfiguration, DefaultListenPath, configure);
        }

        public static void EnableXxlJob(this HttpConfiguration httpConfiguration, string listenPath, Action<JobExecutorConfig> configure = null)
        {
            var jobConfig = new JobExecutorConfig();
            configure?.Invoke(jobConfig);

            httpConfiguration.Routes.MapHttpRoute(
                name: "xxl-job",
                routeTemplate: listenPath,
                defaults: null,
                constraints: new { isXxlJob = new XxlJobConstraint() },
                handler: new XxlJobExecutorHandler(jobConfig)
            );
        }
    }
}
