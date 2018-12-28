using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XxlJob.Core.Executor;

namespace XxlJob.Core.DependencyInjection
{
    public static class XxlJobExecutorBuilderExtensions
    {
        public static IXxlJobExecutorBuilder Configure(this IXxlJobExecutorBuilder builder, IConfiguration config)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            builder.Services.Configure<JobExecutorOption>(config);

            return builder;
        }

        public static IXxlJobExecutorBuilder Configure(this IXxlJobExecutorBuilder builder, Action<JobExecutorOption> configAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configAction == null)
            {
                throw new ArgumentNullException(nameof(configAction));
            }

            builder.Services.Configure<JobExecutorOption>(configAction);

            return builder;
        }

        /// <summary>
        /// 添加默认JobHandlerFactory，默认实现会自动加载所有继承自<see cref="IJobHandler"/>的类
        /// <para>如果没有手动调用此方法，框架内部会自动调用AddDefaultJobHandlerFactory(true)</para>
        /// </summary>
        /// <param name="isJobHandlerSingleton">如果传true则每次执行作业使用同一个<see cref="IJobHandler"/>实例，否则每次生成新的实例</param>        
        public static IXxlJobExecutorBuilder AddDefaultJobHandlerFactory(this IXxlJobExecutorBuilder builder, bool isJobHandlerSingleton)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var lifetime = isJobHandlerSingleton ? ServiceLifetime.Singleton : ServiceLifetime.Transient;
            foreach (var type in DefaultJobHandlerFactory.HandlersTypes.Values)
            {
                builder.Services.TryAdd(ServiceDescriptor.Describe(type, type, lifetime));
            }
            builder.Services.TryAddSingleton<JobHandlerFactory, DefaultJobHandlerFactory>();

            return builder;
        }
    }
}
