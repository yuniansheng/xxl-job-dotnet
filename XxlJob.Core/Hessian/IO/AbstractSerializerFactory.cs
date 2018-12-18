using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{
    /// <summary>
    /// Factory for returning serialization methods.
    /// </summary>
    public abstract class AbstractSerializerFactory
    {
        /// <summary>
        /// Returns the serializer for a class.
        ///
        /// <param name="cl">the class of the object that needs to be serialized.</param>
        ///
        /// <returns>a serializer object for the serialization.</returns>
        /// </summary>
        public abstract ISerializer GetSerializer(Type cl);

        /// <summary>
        /// Returns the deserializer for a class.
        ///
        /// <param name="cl">the class of the object that needs to be deserialized.</param>
        ///
        /// <returns>a deserializer object for the serialization.</returns>
        /// </summary>
        public abstract IDeserializer GetDeserializer(Type cl);
    }

}