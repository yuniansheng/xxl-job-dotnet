using com.xxl.job.core.biz.model;
using com.xxl.job.core.rpc.codec;
using hessiancsharp.io;
using java.lang;
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
    public class Hessian2Test
    {
        private readonly ITestOutputHelper output;

        public Hessian2Test(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RunRequestTest()
        {
            var str = "43302d636f6d2e78786c2e7270632e72656d6f74696e672e6e65742e706172616d732e58786c527063526571756573749809726571756573744964106372656174654d696c6c697354696d650b616363657373546f6b656e09636c6173734e616d650a6d6574686f644e616d650776657273696f6e0e706172616d6574657254797065730a706172616d657465727360302461373166626536362d343166642d343261382d613832622d3366646637613233333564614c0000016813feace7302063646166663831336162663032666665303662653034363962336633656634333020636f6d2e78786c2e6a6f622e636f72652e62697a2e4578656375746f7242697a0372756e4e71105b6a6176612e6c616e672e436c617373430f6a6176612e6c616e672e436c61737391046e616d65613027636f6d2e78786c2e6a6f622e636f72652e62697a2e6d6f64656c2e54726967676572506172616d71075b6f626a656374433027636f6d2e78786c2e6a6f622e636f72652e62697a2e6d6f64656c2e54726967676572506172616d9c056a6f6249640f6578656375746f7248616e646c65720e6578656375746f72506172616d73156578656375746f72426c6f636b53747261746567790f6578656375746f7254696d656f7574056c6f6749640a6c6f674461746554696d08676c7565547970650a676c7565536f757263650e676c756555706461746574696d650e62726f616463617374496e6465780e62726f616463617374546f74616c6292104d6573736167655363686564756c6572000d444953434152445f4c41544552aebd4c0000016813feac3e044245414e004c00000167fe1f6a789091";
            CHessian2Input input = GetHessian2Input(str);

            var request = (RpcRequest)input.ReadObject();
            Assert.Equal("cdaff813abf02ffe06be0469b3f3ef43", request.accessToken);
            Assert.Equal("com.xxl.job.core.biz.ExecutorBiz", request.className);
            Assert.Equal(1546523684071, request.createMillisTime);
            Assert.Equal("run", request.methodName);
            Assert.Equal("a71fbe66-41fd-42a8-a82b-3fdf7a2335da", request.requestId);
            Assert.Null(request.version);

            Assert.Equal("com.xxl.job.core.biz.model.TriggerParam", (request.parameterTypes[0] as Class).name);

            var parameter1 = request.parameters[0] as TriggerParam;
            Assert.Equal(45, parameter1.logId);
            Assert.Equal(0, parameter1.broadcastIndex);
            Assert.Equal(1, parameter1.broadcastTotal);
            Assert.Equal("BEAN", parameter1.glueType);
            Assert.Equal("", parameter1.glueSource);
            Assert.Equal(1546156731000, parameter1.glueUpdatetime);
            Assert.Equal("MessageScheduler", parameter1.executorHandler);
            Assert.Equal(2, parameter1.jobId);
            Assert.Equal("", parameter1.executorParams);
            Assert.Equal("DISCARD_LATER", parameter1.executorBlockStrategy);
            Assert.Equal(1546523683902, parameter1.logDateTim);
        }

        private CHessian2Input GetHessian2Input(string content)
        {
            var buffer = new byte[content.Length / 2];
            for (int i = 0; i < content.Length; i += 2)
            {
                buffer[i / 2] = Convert.ToByte(content.Substring(i, 2), 16);
            }

            output.WriteLine(Encoding.ASCII.GetString(buffer));
            var stream = new MemoryStream(buffer);
            CHessian2Input input = new CHessian2Input(stream);
            return input;
        }
    }
}
