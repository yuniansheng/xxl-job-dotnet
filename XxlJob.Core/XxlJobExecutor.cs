using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core
{
    public class XxlJobExecutor
    {
        private readonly XxlJobExecutorConfig _config;

        public XxlJobExecutor(XxlJobExecutorConfig config)
        {
            _config = config;
        }

        public byte[] HandleRequest(Stream inputStream)
        {
            var buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
