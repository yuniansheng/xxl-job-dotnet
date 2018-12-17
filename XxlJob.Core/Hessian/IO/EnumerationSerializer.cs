using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a JDK 1.2 Enumeration.
/// </summary>
public class EnumerationSerializer : AbstractSerializer {
  private static EnumerationSerializer _serializer;

  public static EnumerationSerializer Create()
  {
    if (_serializer == null)
      _serializer = new EnumerationSerializer();

    return _serializer;
  }
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    Enumeration iter = (Enumeration) obj;

    bool hasEnd = out.WriteListBegin(-1, null);

    while (iter.HasMoreElements()) {
      object value = iter.NextElement();

      out.WriteObject(value);
    }

    if (hasEnd)
      out.WriteListEnd();
  }
}

}