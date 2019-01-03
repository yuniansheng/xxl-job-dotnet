using hessiancsharp.io;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XxlJob.Core.RPC
{
    public class HessianSerializer : ISerializer
    {
        public byte[] Serialize(object obj)
        {
            using (var stream = new MemoryStream())
            {
                var output = new CHessianOutput(stream);
                output.WriteObject(obj);
                return stream.ToArray();
            }
        }

        public object Deserialize(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                var input = new CHessianInput(stream);
                return input.ReadObject();
            }
        }

        public object Deserialize(Stream inputStream)
        {
            var input = new CHessianInput(inputStream);
            return input.ReadObject();
        }
    }
}
