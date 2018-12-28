using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XxlJob.Core.DependencyInjection;
using XxlJob.AspNetCoreHost;
using XxlJob.Core;
using com.xxl.job.core.biz.model;

namespace AspNetCoreSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddXxlJob(xxlJob =>
            {
                xxlJob.Configure(Configuration.GetSection("XxlJob"));
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseXxlJob();

            app.UseMvc();
        }
    }

    public class MessageScheduler : IJobHandler
    {
        public override ReturnT Execute(JobExecutionContext context)
        {
            return ReturnT.CreateSucceededResult("测试job执行成功了!", "执行返回的内容");
        }
    }
}
