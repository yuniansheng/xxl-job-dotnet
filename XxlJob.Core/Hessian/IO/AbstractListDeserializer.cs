using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Deserializing a JDK 1.2 Collection.
    /// </summary>
    public class AbstractListDeserializer : AbstractDeserializer
    {
        public override object ReadObject(AbstractHessianInput input)
        {
            object obj = input.ReadObject();

            if (obj != null)
                throw Error("expected list at " + obj.GetType().Name + " (" + obj + ")");
            else
                throw Error("expected list at null");
        }
    }

}