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
public class ExtSerializerFactory : AbstractSerializerFactory {
  private HashMap _serializerMap = new HashMap();
  private HashMap _deserializerMap = new HashMap();

  /// <summary>
  /// Adds a serializer.
  ///
  /// <param name="cl">the class of the serializer</param>
  /// <param name="serializer">the serializer</param>
  /// </summary>
  public void AddSerializer(Class cl, ISerializer serializer)
  {
    _serializerMap.Put(cl, serializer);
  }

  /// <summary>
  /// Adds a deserializer.
  ///
  /// <param name="cl">the class of the deserializer</param>
  /// <param name="deserializer">the deserializer</param>
  /// </summary>
  public void AddDeserializer(Class cl, IDeserializer deserializer)
  {
    _deserializerMap.Put(cl, deserializer);
  }
  
  /// <summary>
  /// Returns the serializer for a class.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  public ISerializer GetSerializer(Class cl)
  {
    return (ISerializer) _serializerMap.Get(cl);
  }
  
  /// <summary>
  /// Returns the deserializer for a class.
  ///
  /// <param name="cl">the class of the object that needs to be deserialized.</param>
  ///
  /// <returns>a deserializer object for the serialization.</returns>
  /// </summary>
  public IDeserializer GetDeserializer(Class cl)
  {
    return (IDeserializer) _deserializerMap.Get(cl);
  }
}

}