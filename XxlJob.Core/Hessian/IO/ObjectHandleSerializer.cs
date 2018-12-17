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
public class ObjectHandleSerializer : AbstractSerializer {
  public static readonly ISerializer SER = new ObjectHandleSerializer();
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (obj == null)
      out.WriteNull();
    else {
      if (out.AddRef(obj))
        return;
      
      int ref = out.WriteObjectBegin("object");

      if (ref < -1) {
        out.WriteMapEnd();
      }
      else {
        if (ref == -1) {
          out.WriteInt(0);
          out.WriteObjectBegin("object");
        }
      }
    }
  }
}

}