using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{






/// <summary>
/// Serializing a JDK 1.2 java.util.Map.
/// </summary>
public class MapSerializer : AbstractSerializer {
  private bool _isSendJavaType = true;

  /// <summary>
  /// Set true if the java type of the collection should be sent.
  /// </summary>
  public void SetSendJavaType(bool sendJavaType)
  {
    _isSendJavaType = sendJavaType;
  }

  /// <summary>
  /// Return true if the java type of the collection should be sent.
  /// </summary>
  public bool GetSendJavaType()
  {
    return _isSendJavaType;
  }
    
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (out.AddRef(obj))
      return;

    Map map = (Map) obj;

    Type cl = obj.GetClass();
    
    if (cl.Equals(HashMap.class)
        || ! (obj instanceof java.io.Serializable))
      out.WriteMapBegin(null);
    else if (! _isSendJavaType) {
      // hessian/3a19
      for (; cl != null; cl = cl.GetSuperclass()) {
        if (cl.Equals(HashMap.class)) {
          out.WriteMapBegin(null);
          break;
        }
        else if (cl.GetName().StartsWith("java.")) {
          out.WriteMapBegin(cl.GetName());
          break;
        }
      }
      
      if (cl == null)
        out.WriteMapBegin(null);
    }
    else {
      out.WriteMapBegin(cl.GetName());
    }

    Iterator iter = map.EntrySet().Iterator();
    while (iter.HasNext()) {
      Map.Entry entry = (Map.Entry) iter.Next();

      out.WriteObject(entry.GetKey());
      out.WriteObject(entry.GetValue());
    }
    out.WriteMapEnd();
  }
}

}