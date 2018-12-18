using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{







/// <summary>
/// Serializing a JDK 1.2 Collection.
/// </summary>
public class CollectionSerializer : AbstractSerializer
{
  private bool _sendJavaType = true;

  /// <summary>
  /// Set true if the java type of the collection should be sent.
  /// </summary>
  public void SetSendJavaType(bool sendJavaType)
  {
    _sendJavaType = sendJavaType;
  }

  /// <summary>
  /// Return true if the java type of the collection should be sent.
  /// </summary>
  public bool GetSendJavaType()
  {
    return _sendJavaType;
  }
    
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (out.AddRef(obj))
      return;

    Collection list = (Collection) obj;

    Class cl = obj.GetClass();
    bool hasEnd;
    
    if (cl.Equals(ArrayList.class)
        || ! Serializable.class.IsAssignableFrom(cl)) {
      hasEnd = out.WriteListBegin(list.Size(), null);
    }
    else if (! _sendJavaType) {
      hasEnd = false;
      
      // hessian/3a19
      for (; cl != null; cl = cl.GetSuperclass()) {
        if (cl.GetName().StartsWith("java.")) {
          hasEnd = out.WriteListBegin(list.Size(), cl.GetName());
          break;
        }
      }
      
      if (cl == null)
        hasEnd = out.WriteListBegin(list.Size(), null);
    }
    else {
      hasEnd = out.WriteListBegin(list.Size(), obj.GetType().Name);
    }

    Iterator iter = list.Iterator();
    while (iter.HasNext()) {
      object value = iter.Next();

      out.WriteObject(value);
    }

    if (hasEnd)
      out.WriteListEnd();
  }
}

}