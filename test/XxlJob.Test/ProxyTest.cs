using com.xxl.job.core.biz.model;
using com.xxl.job.core.rpc.codec;
using hessiancsharp.io;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XxlJob.Core;
using XxlJob.Core.Executor;

namespace XxlJob.Test
{
    public class ProxyTest
    {
        private readonly ITestOutputHelper output;

        public ProxyTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void DynamicProxyTest()
        {
            //var config = new JobExecutorConfig()
            //{
            //    AccessToken = "cdaff813abf02ffe06be0469b3f3ef43"
            //};
            //var result = new AdminClient(config).Callback(new List<HandleCallbackParam>
            //{
            //    new HandleCallbackParam{logId=2042,logDateTim=1545280643790,executeResult=ReturnT.CreateSucceededResult("test callack") }
            //});
            //output.WriteLine(result.ToString());
        }
    }
}
