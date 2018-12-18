using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Handle for Java Float objects.
/// </summary>
public class FloatHandle : Serializable {
  private float _value;

  private FloatHandle()
  {
  }

  public FloatHandle(float value)
  {
    _value = value;
  }

  public float GetValue()
  {
    return _value;
  }

  public object ReadResolve()
  {
    return new Float(_value);
  }

  public string ToString()
  {
    return GetClass().GetSimpleName() + "[" + _value + "]";
  }
}

}