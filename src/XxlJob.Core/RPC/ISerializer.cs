using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XxlJob.Core.RPC
{
    public interface ISerializer
    {
        byte[] Serialize(object value);

        object Deserialize(byte[] value);

        object Deserialize(Stream inputStream);
    }
}
