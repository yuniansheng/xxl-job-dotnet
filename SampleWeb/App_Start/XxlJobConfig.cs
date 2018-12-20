using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using com.xxl.job.core.biz.model;
using XxlJob.Core;
using XxlJob.WebApiHost;

namespace SampleWeb
{
    public static class XxlJobConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.EnableXxlJob(jobConfig =>
            {
                jobConfig.AdminAddresses.Add("http://172.18.21.144:8080/xxl-job-admin");
                jobConfig.AccessToken = "cdaff813abf02ffe06be0469b3f3ef43";
            });
        }
    }

    public class MessageScheduler : IJobHandler
    {
        public override ReturnT Execute(JobExecutionContext context)
        {
            return ReturnT.CreateSucceededResult("测试job执行成功了!" + Environment.NewLine + "测试多行日志", "执行返回的内容");
        }
    }
}
