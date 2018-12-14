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
using XxlJob.Core.RPC;

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
        public void DeserializeTest()
        {
            var str = "4d:74:00:25:63:6f:6d:2e:78:78:6c:2e:6a:6f:62:2e:63:6f:72:65:2e:72:70:63:2e:63:6f:64:65:63:2e:52:70:63:52:65:71:75:65:73:74:53:00:0d:73:65:72:76:65:72:41:64:64:72:65:73:73:53:00:15:31:37:32:2e:31:38:2e:32:33:2e:31:33:32:3a:31:38:31:2f:6a:6f:62:53:00:10:63:72:65:61:74:65:4d:69:6c:6c:69:73:54:69:6d:65:4c:00:00:01:67:a6:4d:4b:6d:53:00:0b:61:63:63:65:73:73:54:6f:6b:65:6e:53:00:20:63:64:61:66:66:38:31:33:61:62:66:30:32:66:66:65:30:36:62:65:30:34:36:39:62:33:66:33:65:66:34:33:53:00:09:63:6c:61:73:73:4e:61:6d:65:53:00:20:63:6f:6d:2e:78:78:6c:2e:6a:6f:62:2e:63:6f:72:65:2e:62:69:7a:2e:45:78:65:63:75:74:6f:72:42:69:7a:53:00:0a:6d:65:74:68:6f:64:4e:61:6d:65:53:00:03:72:75:6e:53:00:0e:70:61:72:61:6d:65:74:65:72:54:79:70:65:73:56:74:00:10:5b:6a:61:76:61:2e:6c:61:6e:67:2e:43:6c:61:73:73:6c:00:00:00:01:4d:74:00:0f:6a:61:76:61:2e:6c:61:6e:67:2e:43:6c:61:73:73:53:00:04:6e:61:6d:65:53:00:27:63:6f:6d:2e:78:78:6c:2e:6a:6f:62:2e:63:6f:72:65:2e:62:69:7a:2e:6d:6f:64:65:6c:2e:54:72:69:67:67:65:72:50:61:72:61:6d:7a:7a:53:00:0a:70:61:72:61:6d:65:74:65:72:73:56:74:00:07:5b:6f:62:6a:65:63:74:6c:00:00:00:01:4d:74:00:27:63:6f:6d:2e:78:78:6c:2e:6a:6f:62:2e:63:6f:72:65:2e:62:69:7a:2e:6d:6f:64:65:6c:2e:54:72:69:67:67:65:72:50:61:72:61:6d:53:00:05:6a:6f:62:49:64:49:00:00:00:04:53:00:0f:65:78:65:63:75:74:6f:72:48:61:6e:64:6c:65:72:53:00:10:4d:65:73:73:61:67:65:53:63:68:65:64:75:6c:65:72:53:00:0e:65:78:65:63:75:74:6f:72:50:61:72:61:6d:73:53:00:00:53:00:15:65:78:65:63:75:74:6f:72:42:6c:6f:63:6b:53:74:72:61:74:65:67:79:53:00:0d:44:49:53:43:41:52:44:5f:4c:41:54:45:52:53:00:05:6c:6f:67:49:64:49:00:00:01:26:53:00:0a:6c:6f:67:44:61:74:65:54:69:6d:4c:00:00:01:67:a6:4d:4b:6d:53:00:08:67:6c:75:65:54:79:70:65:53:00:04:42:45:41:4e:53:00:0a:67:6c:75:65:53:6f:75:72:63:65:53:00:00:53:00:0e:67:6c:75:65:55:70:64:61:74:65:74:69:6d:65:4c:00:00:01:67:a1:83:29:f8:53:00:0e:62:72:6f:61:64:63:61:73:74:49:6e:64:65:78:49:00:00:00:00:53:00:0e:62:72:6f:61:64:63:61:73:74:54:6f:74:61:6c:49:00:00:00:01:7a:7a:7a";
            var parts = str.Split(':');
            var buffer = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                buffer[i] = Convert.ToByte(parts[i], 16);
            }
            Assert.Equal(660, buffer.Length);

            var stream = new MemoryStream(buffer);
            CHessianInput input = new CHessianInput(stream);
            Hashtable obj = (Hashtable)input.ReadObject();
            Assert.Equal(7, obj.Count);



            var parameter1Type = (Hashtable)(obj["parameterTypes"] as object[])[0];
            Assert.Equal("com.xxl.job.core.biz.model.TriggerParam", parameter1Type["name"]);
            Assert.Equal("run", obj["methodName"]);

            stream.Seek(0, SeekOrigin.Begin);
            var request = (XxlRpcRequest)input.ReadObject(typeof(XxlRpcRequest));
            Assert.Equal("cdaff813abf02ffe06be0469b3f3ef43", request.accessToken);
            Assert.Equal("com.xxl.job.core.biz.ExecutorBiz", request.className);
            Assert.Equal(1544683342701, request.createMillisTime);
            Assert.Equal("run", request.methodName);
            Assert.Null(request.requestId);
            Assert.Null(request.version);

            var parameter1 = request.parameters[0] as Hashtable;
            Assert.Equal(294, parameter1["logId"]);
            Assert.Equal(0, parameter1["broadcastIndex"]);
            Assert.Equal(1, parameter1["broadcastTotal"]);
            Assert.Equal("BEAN", parameter1["glueType"]);
            Assert.Equal("", parameter1["glueSource"]);
            Assert.Equal(1544602987000, parameter1["glueUpdatetime"]);

            Assert.Equal("MessageScheduler", parameter1["executorHandler"]);
            Assert.Equal(4, parameter1["jobId"]);
            Assert.Equal("", parameter1["executorParams"]);
            Assert.Equal("DISCARD_LATER", parameter1["executorBlockStrategy"]);
            Assert.Equal(1544683342701, parameter1["logDateTim"]);
        }
    }
}
