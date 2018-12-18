using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Deserializing a byte stream
    /// </summary>
    public abstract class AbstractStreamDeserializer : AbstractDeserializer
    {
        public new abstract Type GetTargetType();

        /// <summary>
        /// Reads the Hessian 1.0 style map.
        /// </summary>
        public override object ReadMap(AbstractHessianInput input)
        {
            object value = null;

            while (!input.IsEnd())
            {
                string key = input.ReadString();

                if (key.Equals("value"))
                    value = ReadStreamValue(input);
                else
                    input.ReadObject();
            }

            input.ReadMapEnd();

            return value;
        }

        public override object ReadObject(AbstractHessianInput input, object[] fields)
        {
            string[] fieldNames = (string[])fields;

            object value = null;

            for (int i = 0; i < fieldNames.Length; i++)
            {
                if ("value".Equals(fieldNames[i]))
                {
                    value = ReadStreamValue(input);
                    input.AddRef(value);
                }
                else
                {
                    input.ReadObject();
                }
            }

            return value;
        }

        abstract protected object ReadStreamValue(AbstractHessianInput input);
    }

}