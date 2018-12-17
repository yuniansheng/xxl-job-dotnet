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
public class ThrowableSerializer : JavaSerializer {
  public ThrowableSerializer(Type cl, ClassLoader loader)
  {
    Super(cl);
  }
  
    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    Throwable e = (Throwable) obj;

    e.GetStackTrace();

    super.WriteObject(obj, out);
  }
}

}