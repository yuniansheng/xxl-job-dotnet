using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Deserializes a string-valued object like BigDecimal.
    /// </summary>
    public abstract class AbstractStringValueDeserializer : AbstractDeserializer
    {
        abstract protected object Create(string value);

        public override object ReadMap(AbstractHessianInput input)
        {
            string value = null;

            while (!input.IsEnd())
            {
                string key = input.ReadString();

                if (key.Equals("value"))
                    value = input.ReadString();
                else
                    input.ReadObject();
            }

            input.ReadMapEnd();

            object obj = Create(value);

            input.AddRef(obj);

            return obj;
        }

        public override object ReadObject(AbstractHessianInput input, object[] fields)
        {
            string[] fieldNames = (string[])fields;

            string value = null;

            for (int i = 0; i < fieldNames.Length; i++)
            {
                if ("value".Equals(fieldNames[i]))
                    value = input.ReadString();
                else
                    input.ReadObject();
            }

            object obj = Create(value);

            input.AddRef(obj);

            return obj;
        }
    }
}