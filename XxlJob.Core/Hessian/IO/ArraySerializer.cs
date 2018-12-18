using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Serializing a Java array.
    /// </summary>
    public class ArraySerializer : AbstractSerializer
    {
        public void WriteObject(object obj, AbstractHessianOutput output)
        {
            if (output.AddRef(obj))
                return;

            object[] array = (object[])obj;

            bool hasEnd = output.WriteListBegin(array.Length,
                                                GetArrayType(obj.GetType());

            for (int i = 0; i < array.Length; i++)
                output.WriteObject(array[i]);

            if (hasEnd)
                output.WriteListEnd();
        }

        /// <summary>
        /// Returns the &lt;type> name for a &lt;list>.
        /// </summary>
        private string GetArrayType(Type cl)
        {
            if (cl.IsArray)
                return '[' + GetArrayType(cl.GetElementType());

            string name = cl.Name;

            if (name.Equals("java.lang.String"))
                return "string";
            else if (name.Equals("java.lang.Object"))
                return "object";
            else if (name.Equals("java.util.Date"))
                return "date";
            else
                return name;
        }
    }
}