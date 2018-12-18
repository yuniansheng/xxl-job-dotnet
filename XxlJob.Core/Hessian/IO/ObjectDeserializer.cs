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
public class ObjectDeserializer : AbstractDeserializer {
  private Type _cl;

  public ObjectDeserializer(Type cl)
  {
    _cl = cl;
  }

  public Type GetType()
  {
    return _cl;
  }
  
    
  public override object ReadObject(AbstractHessianInput in)
      {
    return in.ReadObject();
  }

    public override object ReadObject(AbstractHessianInput in, object[] fields)
      {
    throw new NotSupportedException(ToString());
  }
  
    
  public override object ReadList(AbstractHessianInput in, int length)
      {
    throw new NotSupportedException(ToString());
  }
  
    
  public override object ReadLengthList(AbstractHessianInput in, int length)
      {
    throw new NotSupportedException(ToString());
  }

    public override string ToString()
  {
    return GetClass().GetSimpleName() + "[" + _cl + "]";
  }
}

}