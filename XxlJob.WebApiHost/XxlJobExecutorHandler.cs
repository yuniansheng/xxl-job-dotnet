using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XxlJob.Core;
using XxlJob.Core.Executor;

namespace XxlJob.WebApiHost
{
    public class XxlJobExecutorHandler : HttpMessageHandler
    {
        private readonly JobExecutor _executor;

        public XxlJobExecutorHandler(JobExecutor executor)
        {
            _executor = executor;
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
}
