using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Serializing a remote object.
/// </summary>
public class StringValueSerializer : AbstractSerializer {
  public static readonly ISerializer SER = new StringValueSerializer();
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (obj == null)
      out.WriteNull();
    else {
      if (out.AddRef(obj))
        return;
      
      Class cl = obj.GetClass();

      int ref = out.WriteObjectBegin(cl.GetName());

      if (ref < -1) {
        out.WriteString("value");
        out.WriteString(obj.ToString());
        out.WriteMapEnd();
      }
      else {
        if (ref == -1) {
          out.WriteInt(1);
          out.WriteString("value");
          out.WriteObjectBegin(cl.GetName());
        }

        out.WriteString(obj.ToString());
      }
    }
  }
}

}