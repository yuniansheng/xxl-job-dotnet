using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Serializing an object. 
    /// </summary>
    public abstract class AbstractSerializer : ISerializer
    {
        public static readonly NullSerializer NULL = new NullSerializer();

        //protected static readonly Logger log = Logger.GetLogger(AbstractSerializer.class.GetName());

        public virtual void WriteObject(object obj, AbstractHessianOutput output)
        {
            if (output.AddRef(obj))
            {
                return;
            }

            try
            {
                object replace = WriteReplace(obj);

                if (replace != null)
                {
                    // out.RemoveRef(obj);

                    output.WriteObject(replace);

                    output.ReplaceRef(replace, obj);

                    return;
                }
            }
            catch (Exception e)
            {
                // log.Log(Level.FINE, e.ToString(), e);
                throw new HessianException(e);
            }

            Type cl = GetClass(obj);

            int refValue = output.WriteObjectBegin(cl.Name);

            if (refValue < -1)
            {
                WriteObject10(obj, output);
            }
            else
            {
                if (refValue == -1)
                {
                    WriteDefinition20(cl, output);

                    output.WriteObjectBegin(cl.Name);
                }

                WriteInstance(obj, output);
            }
        }

        protected object WriteReplace(object obj)
        {
            return null;
        }

        protected Type GetClass(object obj)
        {
            return obj.GetType();
        }

        protected void WriteObject10(object obj, AbstractHessianOutput output)
        {
            throw new NotSupportedException(GetType().Name);
        }

        protected void WriteDefinition20(Type cl, AbstractHessianOutput output)
        {
            throw new NotSupportedException(GetType().Name);
        }

        protected void WriteInstance(object obj, AbstractHessianOutput output)
        {
            throw new NotSupportedException(GetType().Name);
        }

        /// <summary>
        /// The NullSerializer exists as a marker for the factory classes so
        /// they save a null result.
        /// </summary>
        public sealed class NullSerializer : AbstractSerializer
        {
            public override void WriteObject(object obj, AbstractHessianOutput output)
            {
                throw new InvalidOperationException(GetType().Name);
            }
        }
    }
}