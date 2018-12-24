using Microsoft.Extensions.DependencyInjection;
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
        public static IServiceCollection AddXxlJob(this IServiceCollection services, Action<IXxlJobExecutorBuilder> configure)
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

            var builder = new DefaultXxlJobExecutorBuilder(services);
            configure(builder);
            builder.AddDefaultJobHandlerFactory(true);
            return services;
        }
    }
}
