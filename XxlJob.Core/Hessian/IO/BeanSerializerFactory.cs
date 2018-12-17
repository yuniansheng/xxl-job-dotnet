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
public class BeanSerializerFactory : SerializerFactory {
  /// <summary>
  /// Returns the default serializer for a class that isn't matched
  /// directly.  Application can override this method to produce
  /// bean-style serialization instead of field serialization.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  protected ISerializer GetDefaultSerializer(Class cl)
  {
    return new BeanSerializer(cl, GetClassLoader());
  }
  
  /// <summary>
  /// Returns the default deserializer for a class that isn't matched
  /// directly.  Application can override this method to produce
  /// bean-style serialization instead of field serialization.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  protected IDeserializer GetDefaultDeserializer(Class cl)
  {
    return new BeanDeserializer(cl);
  }
}

}