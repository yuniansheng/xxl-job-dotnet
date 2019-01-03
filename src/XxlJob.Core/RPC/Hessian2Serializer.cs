using hessiancsharp.io;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XxlJob.Core.RPC
{
    public class Hessian2Serializer : ISerializer
    {
        public byte[] Serialize(object obj)
        {
            //using (var stream = new MemoryStream())
            //{
            //    var output = new CHessian2Output(stream);
            //    output.WriteObject(obj);
            //    return stream.ToArray();
            //}

            throw new NotImplementedException();
        }

        public object Deserialize(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                var input = new CHessian2Input(stream);
                return input.ReadObject();
            }
        }

        public object Deserialize(Stream inputStream)
        {
            var input = new CHessian2Input(inputStream);
            return input.ReadObject();
        }
    }
}
