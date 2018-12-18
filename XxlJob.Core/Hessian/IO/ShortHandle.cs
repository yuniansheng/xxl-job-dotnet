using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Handle for Java Short objects.
/// </summary>
public class ShortHandle : Serializable {
  private short _value;

  private ShortHandle()
  {
  }

  public ShortHandle(short value)
  {
    _value = value;
  }

  public short GetValue()
  {
    return _value;
  }

  public object ReadResolve()
  {
    return new Short(_value);
  }

  public string ToString()
  {
    return GetClass().GetSimpleName() + "[" + _value + "]";
  }
}

}