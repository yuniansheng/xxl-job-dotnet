using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a JDK 1.2 Iterator.
/// </summary>
public class IteratorSerializer : AbstractSerializer {
  private static IteratorSerializer _serializer;

  public static IteratorSerializer Create()
  {
    if (_serializer == null)
      _serializer = new IteratorSerializer();

    return _serializer;
  }
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    Iterator iter = (Iterator) obj;

    bool hasEnd = out.WriteListBegin(-1, null);

    while (iter.HasNext()) {
      object value = iter.Next();

      out.WriteObject(value);
    }

    if (hasEnd)
      out.WriteListEnd();
  }
}

}