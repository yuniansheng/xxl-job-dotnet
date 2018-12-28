using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace XxlJob.Core.DependencyInjection
{
    internal class DefaultXxlJobExecutorBuilder : IXxlJobExecutorBuilder
    {
        public DefaultXxlJobExecutorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
