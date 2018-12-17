using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Serializing an object for known object types.
    /// </summary>
    public class AbstractMapDeserializer : AbstractDeserializer
    {
        public Type GetTargetType()
        {
            return typeof(Hashtable);
        }

        public object ReadObject(AbstractHessianInput input)
        {
            object obj = input.ReadObject();

            if (obj != null)
                throw Error("expected map/object at " + obj.GetType().Name + " (" + obj + ")");
            else
                throw Error("expected map/object at null");
        }
    }
}