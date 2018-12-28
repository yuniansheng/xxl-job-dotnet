using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace XxlJob.AspNetCoreHost
{
    public static class XxlJobAppBuilderExtensions
    {
        public static IApplicationBuilder UseXxlJob(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<XxlJobExecutorMiddleware>();
        }

        public static IApplicationBuilder MapXxlJob(this IApplicationBuilder app, PathString pathMatch)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.Map(pathMatch, appBuilder => appBuilder.UseMiddleware<XxlJobExecutorMiddleware>());
        }
    }
}
