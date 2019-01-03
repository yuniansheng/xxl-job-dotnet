using System;
using System.Collections.Generic;
using System.Text;

namespace XxlJob.Core.RPC
{
    public enum SerializeProtocol
    {
        /// <summary>
        /// hessian，如果是xxl-job 1.9.1或之前的版本，请选择此项
        /// </summary>
        Hessian = 0,

        /// <summary>
        /// hessian2，如果是xxl-job 1.9.2或之后的版本，请选择此项
        /// </summary>
        Hessian2 = 1
    }

    internal static class SerializeProtocolExtensions
    {
        public static ISerializer GetSerializer(this SerializeProtocol protocol)
        {
            if (protocol == SerializeProtocol.Hessian)
            {
                return new HessianSerializer();
            }
            else
            {
                return new Hessian2Serializer();
            }
        }
    }
}
