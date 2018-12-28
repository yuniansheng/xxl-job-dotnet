using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using XxlJob.Core.Executor;

namespace XxlJob.AspNetCoreHost
{
    public class XxlJobExecutorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JobExecutor _executor;
        private readonly ILogger _logger;

        public XxlJobExecutorMiddleware(RequestDelegate next, IServiceProvider services)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
            _executor = services.GetService<JobExecutor>();
            _logger = services.GetService<ILoggerFactory>().CreateLogger<XxlJobExecutorMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsRequestMatch(context))
            {
                try
                {
                    byte[] responseBytes = _executor.HandleRequest(context.Request.Body);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "text/html;charset=UTF-8";
                    await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "XxlJobExecutorMiddleware handle request error.");
                    throw;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private bool IsRequestMatch(HttpContext context)
        {
            return context.Request.Method.Equals("POST") &&
                context.Request.ContentType == "application/octet-stream";
        }
    }
}
