using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace XxlJob.Core.DependencyInjection
{
    public interface IXxlJobExecutorBuilder
    {
        IServiceCollection Services { get; }
    }
}
