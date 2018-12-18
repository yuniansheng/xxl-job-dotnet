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
public class EnumerationDeserializer : AbstractListDeserializer {
  private static EnumerationDeserializer _deserializer;

  public static EnumerationDeserializer Create()
  {
    if (_deserializer == null)
      _deserializer = new EnumerationDeserializer();

    return _deserializer;
  }
  
  public object ReadList(AbstractHessianInput in, int length)
      {
    Vector list = new Vector();

    in.AddRef(list);

    while (! in.IsEnd())
      list.Add(in.ReadObject());

    in.ReadEnd();

    return list.Elements();
  }
}



}