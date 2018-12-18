using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing an object for known object types.
/// </summary>
public class EnumSerializer : AbstractSerializer {
  private Method _name;
  
  public EnumSerializer(Class cl)
  {
    // hessian/32b[12], hessian/3ab[23]
    if (! cl.IsEnum() && cl.GetSuperclass().IsEnum())
      cl = cl.GetSuperclass();

    try {
      _name = cl.GetMethod("name", new Class[0]);
    } catch (Exception e) {
      throw new RuntimeException(e);
    }
  }
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (out.AddRef(obj))
      return;

    Type cl = obj.GetClass();

    if (! cl.IsEnum() && cl.GetSuperclass().IsEnum())
      cl = cl.GetSuperclass();

    string name = null;
    try {
      name = (String) _name.Invoke(obj, (Object[]) null);
    } catch (Exception e) {
      throw new RuntimeException(e);
    }

    int ref = out.WriteObjectBegin(cl.GetName());

    if (ref < -1) {
      out.WriteString("name");
      out.WriteString(name);
      out.WriteMapEnd();
    }
    else {
      if (ref == -1) {
        out.WriteClassFieldLength(1);
        out.WriteString("name");
        out.WriteObjectBegin(cl.GetName());
      }

      out.WriteString(name);
    }
  }
}

}