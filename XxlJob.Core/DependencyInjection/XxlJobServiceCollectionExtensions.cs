using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XxlJob.Core.Executor;
using XxlJob.Core.Threads;

namespace XxlJob.Core.DependencyInjection
{
    public static class XxlJobServiceCollectionExtensions
    {
        public static IXxlJobExecutorBuilder AddXxlJob(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddLogging();
            services.AddOptions();

            services.AddSingleton<JobExecutor>();
            services.AddSingleton<JobThreadFactory>();
            services.AddTransient<JobThread>();

            services.AddSingleton<TriggerCallbackThread>();
            services.AddSingleton<AdminClient>();
            services.AddSingleton<HandleCallbackParamRepository>();

            services.Configure<JobExecutorOption>(option =>
            {
                option.LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.XxlLogsDefaultRootDirectory);
            });

            var builder = new DefaultXxlJobExecutorBuilder(services);
            return builder;
        }
    }
}
