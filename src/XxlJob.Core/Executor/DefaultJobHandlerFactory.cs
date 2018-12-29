using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace XxlJob.Core.Executor
{
    /// <summary>
    /// 默认JobHandlerFactory，初次运行job时查找所有继承自IJobHandler的类并实例化，后续job执行共享之前创建的实例
    /// </summary>
    internal class DefaultJobHandlerFactory : JobHandlerFactory
    {
        private static readonly Lazy<Dictionary<string, Type>> _handlersTypes = new Lazy<Dictionary<string, Type>>(LoadJobHandlers, LazyThreadSafetyMode.ExecutionAndPublication);

        public static Dictionary<string, Type> HandlersTypes
        {
            get
            {
                return _handlersTypes.Value;
            }
        }

        private readonly IServiceProvider _services;

        public DefaultJobHandlerFactory(IServiceProvider services)
        {
            _services = services;
        }

        public override IJobHandler GetJobHandler(string handlerName)
        {
            Type handlerType;
            if (_handlersTypes.Value.TryGetValue(handlerName, out handlerType))
            {
                return (IJobHandler)_services.GetService(handlerType);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(handlerName), $"can not find the job handler type:{handlerName}");
            }
        }

        private static Dictionary<string, Type> LoadJobHandlers()
        {
            var handlers = new Dictionary<string, Type>();
            var interfaceType = typeof(IJobHandler);
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => asm != interfaceType.Assembly)
                .SelectMany(asm => GetLoadableTypes(asm).Where(t => !t.IsInterface && !t.IsAbstract && interfaceType.IsAssignableFrom(t)))
                .ToDictionary(t => t.Name, t => t);
            return handlerTypes;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }
}
