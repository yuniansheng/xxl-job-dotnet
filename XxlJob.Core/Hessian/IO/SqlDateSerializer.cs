using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a sql date object.
/// </summary>
public class SqlDateSerializer : AbstractSerializer
{
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (obj == null)
      out.WriteNull();
    else {
      Class cl = obj.GetClass();

      if (out.AddRef(obj))
        return;
      
      int ref = out.WriteObjectBegin(cl.GetName());

      if (ref < -1) {
        out.WriteString("value");
        out.WriteUTCDate(((Date) obj).GetTime());
        out.WriteMapEnd();
      }
      else {
        if (ref == -1) {
          out.WriteInt(1);
          out.WriteString("value");
          out.WriteObjectBegin(cl.GetName());
        }

        out.WriteUTCDate(((Date) obj).GetTime());
      }
    }
  }
}

}