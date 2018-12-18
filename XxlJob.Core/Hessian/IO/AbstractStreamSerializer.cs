using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Serializing an object containing a byte stream.
    /// </summary>
    public abstract class AbstractStreamSerializer : AbstractSerializer
    {
        /// <summary>
        /// Writes the object to the output stream.
        /// </summary>
        public override void WriteObject(object obj, AbstractHessianOutput output)
        {
            if (output.AddRef(obj))
            {
                return;
            }

            int refValue = output.WriteObjectBegin(GetClassName(obj));

            if (refValue < -1)
            {
                output.WriteString("value");

                Stream inputStream = null;

                try
                {
                    inputStream = GetInputStream(obj);
                }
                catch (Exception e)
                {
                    //log.Log(Level.WARNING, e.ToString(), e);
                }

                if (inputStream != null)
                {
                    try
                    {
                        output.WriteByteStream(inputStream);
                    }
                    finally
                    {
                        inputStream.Close();
                    }
                }
                else
                {
                    output.WriteNull();
                }

                output.WriteMapEnd();
            }
            else
            {
                if (refValue == -1)
                {
                    output.WriteClassFieldLength(1);
                    output.WriteString("value");

                    output.WriteObjectBegin(GetClassName(obj));
                }

                Stream inputStream = null;

                try
                {
                    inputStream = GetInputStream(obj);
                }
                catch (Exception e)
                {
                    //log.Log(Level.WARNING, e.ToString(), e);
                }

                try
                {
                    if (inputStream != null)
                        output.WriteByteStream(inputStream);
                    else
                        output.WriteNull();
                }
                finally
                {
                    if (inputStream != null)
                        inputStream.Close();
                }
            }
        }

        protected string GetClassName(object obj)
        {
            return obj.GetType().FullName;
        }

        abstract protected Stream GetInputStream(object obj);
    }
}