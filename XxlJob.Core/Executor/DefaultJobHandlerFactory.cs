using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    /// <summary>
    /// 默认JobHandlerFactory，初次运行job时查找所有继承自IJobHandler的类并实例化，后续job执行共享之前创建的实例
    /// </summary>
    internal class DefaultJobHandlerFactory : JobHandlerFactory
    {
        private readonly Lazy<Dictionary<string, IJobHandler>> _handlers;

        public DefaultJobHandlerFactory()
        {
            _handlers = new Lazy<Dictionary<string, IJobHandler>>(LoadJobHandlers, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public override IJobHandler GetJobHandler(string handlerName)
        {
            IJobHandler handler;
            _handlers.Value.TryGetValue(handlerName, out handler);
            return handler;
        }

        private Dictionary<string, IJobHandler> LoadJobHandlers()
        {
            var handlers = new Dictionary<string, IJobHandler>();
            var interfaceType = typeof(IJobHandler);
            var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => asm != interfaceType.Assembly)
                .SelectMany(asm => asm.GetTypes().Where(t => interfaceType.IsAssignableFrom(t)));
            foreach (var type in handlerTypes)
            {
                var instance = (IJobHandler)Activator.CreateInstance(type);
                handlers.Add(type.Name, instance);
            }
            return handlers;
        }
    }
}
