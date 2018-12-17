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
public class UnsafeUnsharedSerializer : UnsafeSerializer
{
  private static readonly Logger log
    = Logger.GetLogger(UnsafeUnsharedSerializer.class.GetName());
  
  public UnsafeUnsharedSerializer(Type cl)
  {
    Super(cl);
  }
  
    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    bool oldUnshared = out.SetUnshared(true);
    
    try {
      super.WriteObject(obj, out);
    } readonlyly {
      out.SetUnshared(oldUnshared);
    }
  }
}

}