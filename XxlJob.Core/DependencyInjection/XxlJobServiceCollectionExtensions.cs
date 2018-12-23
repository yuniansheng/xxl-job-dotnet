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
        public static IServiceCollection AddXxlJob(this IServiceCollection services)
        {
            return AddXxlJob(services, option => { });
        }

        public static IServiceCollection AddXxlJob(this IServiceCollection services, Action<JobExecutorConfig> configure)
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
            services.AddSingleton<JobHandlerFactory, DefaultJobHandlerFactory>();

            services.AddSingleton<TriggerCallbackThread>();
            services.AddSingleton<AdminClient>();
            services.AddSingleton<HandleCallbackParamRepository>();

            services.Configure<JobExecutorConfig>(option =>
            {
                option.LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.XxlLogsDefaultRootDirectory);
            });
            services.Configure<JobExecutorConfig>(configure);

            return services;
        }
    }
}
