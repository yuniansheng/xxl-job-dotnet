using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using com.xxl.job.core.biz.model;
using XxlJob.Core;
using XxlJob.Core.Executor;

namespace SampleWeb
{
    public static class XxlJobConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.EnableXxlJob(jobConfig =>
            {
                //jobConfig.AccessToken = "test invalid token";
            });
        }
    }



    public static class HttpConfigurationExtensions
    {
        private static readonly string DefaultListenPath = "job";


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
                constraints: null,
                handler: new XxlJobExecutorHandler(jobConfig)
            );
        }
    }

    public class XxlJobExecutorHandler : HttpMessageHandler
    {
        private readonly JobExecutor _executor;

        public XxlJobExecutorHandler(JobExecutorConfig config)
        {
            _executor = new JobExecutor(config);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var inputStream = request.Content.ReadAsStreamAsync().Result;
            byte[] responseBytes = _executor.HandleRequest(inputStream);

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(responseBytes);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html") { CharSet = "UTF-8" };
            return Task.FromResult(response);
        }
    }

    public class MessageScheduler : IJobHandler
    {
        public override ReturnT Execute(JobExecutionContext context)
        {
            File.AppendAllText(@"D:\job.txt", $"{DateTime.Now.ToString("O")} execute job" + Environment.NewLine);
            return ReturnT.SUCCESS;
        }
    }
}
