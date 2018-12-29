# xxl-job-dotnet
此项目目的是为xxl-job提供一个.net版的任务执行器，以便.net项目能够享受xxl-job带来的便利，那么xxl-job是什么呢，你可以参考作者的github [xxl-job](https://github.com/xuxueli/xxl-job)，其中有详细的介绍，总而言之，它是一个分布式的任务调度平台，让你可以轻松地完成定时任务的开发，尤其是在分布式环境中，xxl-job解决了许多分布式环境下的难题。\
那xxl-job-net又是做什么的呢？如果你了解xxl-job可以忽略下面的解释。首先要大致说一下xxl-job的架构，这里引用了xxl-job官网的一张图片，不要被图中复杂的关系吓到，你只要关注`调度器` 与 `执行器服务`(图中橙色部分)，简单讲，调度器与执行器服务是两个不同的程序，甚至会位于不同机器，调度器只负责作业调度，触发并请求执行器执行任务，执行器则负责真正的作业逻辑，这与hangfire是不同的。由于xxl-job是java编写的，官方只提供了java版本的执行器，.net项目无法使用xxl-job，因此xxl-job-net实现了.net版本的执行器，让.net项目也可以方便地使用xxl-job。

![blockchain](https://raw.githubusercontent.com/xuxueli/xxl-job/master/doc/images/img_Qohm.png)

# aspnet webapi承载方式
在webapi项目中引用XxlJob.WebApiHost这个nuget包，因为XxlJob.Core是面向netstandard2.0的，所以 .net framework版本要求4.6.1+，然后按如下示例更改代码

```C#
//在Application_Start中加入XxlJob配置
public class WebApiApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(XxlJobConfig.Register);
        //other config
    }
}

//添加XxlJobConfig类
using com.xxl.job.core.biz.model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XxlJob.Core;
using XxlJob.Core.DependencyInjection;
using XxlJob.WebApiHost;

public static class XxlJobConfig
{
    public static void Register(HttpConfiguration config)
    {
        var services = new ServiceCollection()
            .AddXxlJob(xxlJob =>
            {
                xxlJob
                    .Configure(option =>
                    {
                        option.AdminAddresses.Add("http://localhost:8080/xxl-job-admin");
                    });
            });

        config.EnableXxlJob(services.BuildServiceProvider());
    }
}
```
默认会监听根路径用于接收调度器的请求，如果需要配置其它路由可以使用EnableXxlJob的重载方法进行配置，如下，将会监听/xxl-job这个路径的请求
```c#
config.EnableXxlJob(services.BuildServiceProvider(),"xxl-job")
```


# aspnet core承载方式
在aspnetcore项目中引用XxlJob.AspNetCoreHost这个nuget包，然后在Startup中配置xxl-job相关的服务和管道，这里使用了配置文件方式加载xxl-job配置，要求 .net core 2.0+ 版本
```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddXxlJob(xxlJob =>
    {
        xxlJob.Configure(Configuration.GetSection("XxlJob"));
    });
    services.AddMvc();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseXxlJob();

    app.UseMvc();
}
```
上面代码中```Configuration.GetSection("XxlJob")```从配置文件中读取配置初始化xxl-job
```json
{
  "XxlJob": {
    "AdminAddresses": [ "http://localhost:8080/xxl-job-admin" ]
  }
}
```

# xxl-job配置
AdminAddress 如上述示例代码中的配置，它表示调度器的地址，如果调度器是集群部署的，则可以添加多个，执行器内部会自动选择，此配置是必须的\
除此之外还可以对其它属性进行配置\
AccessToken：用于验证调度器和执行器之间的请求，双方要么都配置要么都不配置\
LogPath：任务执行日志是以文件方式存储在执行器所在机器上的，此参数配置日志文件位置，默认为`Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "xxl-job-logs")`\
LogRetentionDays：日志清理程序会定期清除任务执行的日志文件，此参数控制文件保留天数，默认30天\
xxl-job-net 大量使用 aspnet core的特性，如上述示例配置就是使用的aspnet core中通用的配置方式，你可以通过`xxlJob.Configure(Action<JobExecutorOption> configAction)`方法使用委托方式配置，也可以直接传入创建好的IConfiguration实例进行配置`xxlJob.Configure(IConfiguration config)`，如果了解更多，请参考aspnet core中配置的使用

# 如何编写任务处理代码
新建一个JobHandler继承自IJobHandler，在Execute中执行你的业务逻辑就行了，context包含任务执行参数、任务分片参数，JobLogger可用于记录任务执行日志，执行日志是保存在本地文件中的，在xxl-job-admin上可以查看这里记录的日志。默认会加载所以继承自IJobHandler的类，无需额外配置，JobHandler写好后，在xxl-job-admin中配置任务即可，**运行模式** 选择 **BEAN模式**，**JobHandler** 填写此处的类名(示例中是TestHandler)，必须相同，否则无法触发任务
```c#
public class TestHandler : IJobHandler
{
    public override ReturnT Execute(JobExecutionContext context)
    {
        JobLogger.Log("任务开始执行");
        JobLogger.Log("任务执行结束");
        return ReturnT.CreateSucceededResult("测试job执行成功了!", "执行返回的内容");
    }
}
```

# 如何查看xxl-job-net组件本身的异常日志
注意这里强调了是xxl-job-net组件本身的异常日志，而不是任务执行日志，任务执行日志是记录在本地文件中的，可以在xxl-job-admin中查看，这里说的是xxl-job-net内部发生异常，如果xxl-job-net未按预期运行，如何查看日志以便定位问题呢？xxl-job-net使用了Microsoft.Extensions.Logging组件，你可以通过`ServiceCollection`的扩展方法配置日志提供程序，具体方法不在这里介绍，如果你使用过aspnet core应该对xxl-job-net的配置方式不会感到陌生，否则你应该看看asnet core中[依赖注入](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/dependency-injection)、[配置](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/configuration)、[选项](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/configuration/options)、[日志记录](https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/logging)等章节，然后使用起来会更顺手

# 其它说明
* 目前只支持xxl-job 1.9.1版本的调度器端，因为1.9.2+使用了hessian2协议目前还不支持
* 目前不支持执行器自动注册，xxl-job本身的注册机制在有多个ip地址时往往达不到效果，所以暂时不实现此功能，需要在xx-job-admin中手动注册执行器