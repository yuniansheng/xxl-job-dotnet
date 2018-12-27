using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace XxlJob.WebApiHost
{
    internal class XxlJobExecutorHandler : HttpMessageHandler
    {
        private readonly JobExecutor _executor;
        private readonly ILogger _logger;

        public XxlJobExecutorHandler(IServiceProvider services)
        {
            _executor = services.GetService<JobExecutor>();
            _logger = services.GetService<ILoggerFactory>().CreateLogger<XxlJobExecutorHandler>();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var inputStream = request.Content.ReadAsStreamAsync().Result;
                byte[] responseBytes = _executor.HandleRequest(inputStream);

                var response = request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(responseBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html") { CharSet = "UTF-8" };
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XxlJobExecutorHandler handle request error.");
                var response = request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
                return Task.FromResult(response);
            }

        }
    }
}
