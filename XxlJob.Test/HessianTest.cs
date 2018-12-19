using com.xxl.job.core.biz.model;
using com.xxl.job.core.rpc.codec;
using hessiancsharp.io;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace XxlJob.Test
{
    public class HessianTest
    {
        private readonly ITestOutputHelper output;

        public HessianTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RunRequestTest()
        {
            var str = "4d740025636f6d2e78786c2e6a6f622e636f72652e7270632e636f6465632e5270635265717565737453000d736572766572416464726573735300153137322e31382e32332e3133323a3138312f6a6f625300106372656174654d696c6c697354696d654c00000167a64d4b6d53000b616363657373546f6b656e5300206364616666383133616266303266666530366265303436396233663365663433530009636c6173734e616d65530020636f6d2e78786c2e6a6f622e636f72652e62697a2e4578656375746f7242697a53000a6d6574686f644e616d6553000372756e53000e706172616d657465725479706573567400105b6a6176612e6c616e672e436c6173736c000000014d74000f6a6176612e6c616e672e436c6173735300046e616d65530027636f6d2e78786c2e6a6f622e636f72652e62697a2e6d6f64656c2e54726967676572506172616d7a7a53000a706172616d6574657273567400075b6f626a6563746c000000014d740027636f6d2e78786c2e6a6f622e636f72652e62697a2e6d6f64656c2e54726967676572506172616d5300056a6f624964490000000453000f6578656375746f7248616e646c65725300104d6573736167655363686564756c657253000e6578656375746f72506172616d735300005300156578656375746f72426c6f636b537472617465677953000d444953434152445f4c415445525300056c6f674964490000012653000a6c6f674461746554696d4c00000167a64d4b6d530008676c7565547970655300044245414e53000a676c7565536f7572636553000053000e676c756555706461746574696d654c00000167a18329f853000e62726f616463617374496e646578490000000053000e62726f616463617374546f74616c49000000017a7a7a";
            CHessianInput input = GetHessianInput(str);

            var request = (RpcRequest)input.ReadObject();
            Assert.Equal("172.18.23.132:181/job", request.serverAddress);
            Assert.Equal("cdaff813abf02ffe06be0469b3f3ef43", request.accessToken);
            Assert.Equal("com.xxl.job.core.biz.ExecutorBiz", request.className);
            Assert.Equal(1544683342701, request.createMillisTime);
            Assert.Equal("run", request.methodName);
            Assert.Null(request.requestId);
            Assert.Null(request.version);

            Assert.Equal("com.xxl.job.core.biz.model.TriggerParam", (request.parameterTypes[0] as Hashtable)["name"]);

            var parameter1 = request.parameters[0] as TriggerParam;
            Assert.Equal(294, parameter1.logId);
            Assert.Equal(0, parameter1.broadcastIndex);
            Assert.Equal(1, parameter1.broadcastTotal);
            Assert.Equal("BEAN", parameter1.glueType);
            Assert.Equal("", parameter1.glueSource);
            Assert.Equal(1544602987000, parameter1.glueUpdatetime);
            Assert.Equal("MessageScheduler", parameter1.executorHandler);
            Assert.Equal(4, parameter1.jobId);
            Assert.Equal("", parameter1.executorParams);
            Assert.Equal("DISCARD_LATER", parameter1.executorBlockStrategy);
            Assert.Equal(1544683342701, parameter1.logDateTim);
        }

        [Fact]
        public void KillRequestTest()
        {
            var str = "4d740025636f6d2e78786c2e6a6f622e636f72652e7270632e636f6465632e5270635265717565737453000d736572766572416464726573735300173137322e31382e32332e3133323a33323930302f6a6f625300106372656174654d696c6c697354696d654c00000167c51f4dc853000b616363657373546f6b656e5300206364616666383133616266303266666530366265303436396233663365663433530009636c6173734e616d65530020636f6d2e78786c2e6a6f622e636f72652e62697a2e4578656375746f7242697a53000a6d6574686f644e616d655300046b696c6c53000e706172616d657465725479706573567400105b6a6176612e6c616e672e436c6173736c000000014d74000f6a6176612e6c616e672e436c6173735300046e616d65530003696e747a7a53000a706172616d6574657273567400075b6f626a6563746c0000000149000000047a7a";
            CHessianInput input = GetHessianInput(str);

            var request = (RpcRequest)input.ReadObject();
            Assert.Equal("com.xxl.job.core.biz.ExecutorBiz", request.className);
            Assert.Equal("kill", request.methodName);
            Assert.Equal(4, (int)request.parameters[0]);
        }

        [Fact]
        public void LogRequestTest()
        {
            var str = "4d740025636f6d2e78786c2e6a6f622e636f72652e7270632e636f6465632e5270635265717565737453000d736572766572416464726573735300133137322e31382e32332e3133323a32383838325300106372656174654d696c6c697354696d654c00000167bad4d8cd53000b616363657373546f6b656e5300206364616666383133616266303266666530366265303436396233663365663433530009636c6173734e616d65530020636f6d2e78786c2e6a6f622e636f72652e62697a2e4578656375746f7242697a53000a6d6574686f644e616d655300036c6f6753000e706172616d657465725479706573567400105b6a6176612e6c616e672e436c6173736c000000034d74000f6a6176612e6c616e672e436c6173735300046e616d655300046c6f6e677a4d74000f6a6176612e6c616e672e436c6173735300046e616d65530003696e747a52000000037a53000a706172616d6574657273567400075b6f626a6563746c000000034c00000167ba8bbfa0490000059549000000017a7a";
            CHessianInput input = GetHessianInput(str);

            var request = (RpcRequest)input.ReadObject();
            Assert.Equal("com.xxl.job.core.biz.ExecutorBiz", request.className);
            Assert.Equal("log", request.methodName);
            Assert.Equal(1545022980000, (long)request.parameters[0]);
            Assert.Equal(1429, (int)request.parameters[1]);
            Assert.Equal(1, (int)request.parameters[2]);
        }

        [Fact]
        public void CallbackTest()
        {
            var str = "4d740025636f6d2e78786c2e6a6f622e636f72652e7270632e636f6465632e5270635265717565737453000d7365727665724164647265737353002b687474703a2f2f3137322e31382e32312e3134343a383038302f78786c2d6a6f622d61646d696e2f6170695300106372656174654d696c6c697354696d654c00000167c62501b653000b616363657373546f6b656e5300206364616666383133616266303266666530366265303436396233663365663433530009636c6173734e616d6553001d636f6d2e78786c2e6a6f622e636f72652e62697a2e41646d696e42697a53000a6d6574686f644e616d6553000863616c6c6261636b53000e706172616d657465725479706573567400105b6a6176612e6c616e672e436c6173736c000000014d74000f6a6176612e6c616e672e436c6173735300046e616d6553000e6a6176612e7574696c2e4c6973747a7a53000a706172616d6574657273567400075b6f626a6563746c00000001566c000000014d74002e636f6d2e78786c2e6a6f622e636f72652e62697a2e6d6f64656c2e48616e646c6543616c6c6261636b506172616d5300056c6f67496449000007f653000d65786563757465526573756c744d740022636f6d2e78786c2e6a6f622e636f72652e62697a2e6d6f64656c2e52657475726e54530004636f646549000000c85300036d736753000ae585b1e7a1aee8aea4e4ba86302f30e69da1e6b688e681af530007636f6e74656e744e7a7a7a7a7a";
            CHessianInput input = GetHessianInput(str);
            var request = (RpcRequest)input.ReadObject();
            Assert.Equal("http://172.18.21.144:8080/xxl-job-admin/api", request.serverAddress);
            Assert.Equal("cdaff813abf02ffe06be0469b3f3ef43", request.accessToken);
            Assert.Equal("com.xxl.job.core.biz.AdminBiz", request.className);
            Assert.Equal("callback", request.methodName);

            var callback = (request.parameters[0] as ArrayList)[0] as HandleCallbackParam;
            Assert.Equal(2038, callback.logId);
            Assert.Equal(200, callback.executeResult.code);
            Assert.Equal("共确认了0/0条消息", callback.executeResult.msg);
            Assert.Null(callback.executeResult.content);
        }

        private CHessianInput GetHessianInput(string content)
        {
            var buffer = new byte[content.Length / 2];
            for (int i = 0; i < content.Length; i += 2)
            {
                buffer[i / 2] = Convert.ToByte(content.Substring(i, 2), 16);
            }

            output.WriteLine(Encoding.ASCII.GetString(buffer));
            var stream = new MemoryStream(buffer);
            CHessianInput input = new CHessianInput(stream);
            return input;
        }
    }
}
