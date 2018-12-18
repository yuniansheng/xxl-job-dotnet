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
public class ClassSerializer : AbstractSerializer {
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    Class cl = (Class) obj;

    if (cl == null) {
      out.WriteNull();
    }
    else if (out.AddRef(obj)) {
      return;
    }
    else {
      int ref = out.WriteObjectBegin("java.lang.Class");

      if (ref < -1) {
        out.WriteString("name");
        out.WriteString(cl.GetName());
        out.WriteMapEnd();
      }
      else {
        if (ref == -1) {
          out.WriteInt(1);
          out.WriteString("name");
          out.WriteObjectBegin("java.lang.Class");
        }

        out.WriteString(cl.GetName());
      }
    }
  }
}

}