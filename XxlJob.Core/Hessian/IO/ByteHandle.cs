using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Handle for Java Byte objects.
/// </summary>
public class ByteHandle : Serializable {
  private byte _value;

  private ByteHandle()
  {
  }

  public ByteHandle(byte value)
  {
    _value = value;
  }

  public byte GetValue()
  {
    return _value;
  }

  public object ReadResolve()
  {
    return new Byte(_value);
  }

  public string ToString()
  {
    return GetClass().GetSimpleName() + "[" + _value + "]";
  }
}

}