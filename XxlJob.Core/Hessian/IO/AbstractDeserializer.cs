using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Deserializing an object. 
    /// </summary>
    public class AbstractDeserializer : IDeserializer
    {
        public static readonly NullDeserializer NULL = new NullDeserializer();

        public virtual Type GetTargetType()
        {
            return typeof(object);
        }

        public bool IsReadResolve()
        {
            return false;
        }

        public virtual object ReadObject(AbstractHessianInput input)
        {
            object obj = input.ReadObject();

            string className = GetType().Name;

            if (obj != null)
                throw Error(className + ": unexpected object " + obj.GetType().Name + " (" + obj + ")");
            else
                throw Error(className + ": unexpected null value");
        }

        public object ReadList(AbstractHessianInput input, int length)
        {
            throw new NotSupportedException(ToString());
        }

        public object ReadLengthList(AbstractHessianInput input, int length)
        {
            throw new NotSupportedException(ToString());
        }

        public virtual object ReadMap(AbstractHessianInput input)
        {
            object obj = input.ReadObject();

            string className = GetType().Name;

            if (obj != null)
                throw Error(className + ": unexpected object " + obj.GetType().Name + " (" + obj + ")");
            else
                throw Error(className + ": unexpected null value");
        }

        /// <summary>
        /// Creates the field array for a class. The default
        /// implementation returns a String[] array.
        /// <param name="len">number of items in the array</param>
        /// <returns>the new empty array</returns>
        /// </summary>
        public object[] CreateFields(int len)
        {
            return new string[len];
        }

        /// <summary>
        /// Creates a field value class. The default
        /// implementation returns the String.
        /// <param name="len">number of items in the array</param>
        /// <returns>the new empty array</returns>
        /// </summary>
        public object CreateField(string name)
        {
            return name;
        }

        public object ReadObject(AbstractHessianInput input, string[] fieldNames)
        {
            return ReadObject(input, (object[])fieldNames);
        }

        /// <summary>
        /// Reads an object instance from the input stream
        /// </summary>
        public virtual object ReadObject(AbstractHessianInput input, object[] fields)
        {
            throw new NotSupportedException(ToString());
        }

        protected HessianProtocolException Error(string msg)
        {
            return new HessianProtocolException(msg);
        }

        protected string CodeName(int ch)
        {
            if (ch < 0)
                return "end of file";
            else
                return "0x" + (ch & 0xff).ToString("x");
        }

        /// <summary>
        /// The NullDeserializer exists as a marker for the factory classes so
        /// they save a null result.
        /// </summary>
        public sealed class NullDeserializer : AbstractDeserializer
        {
        }
    }

}