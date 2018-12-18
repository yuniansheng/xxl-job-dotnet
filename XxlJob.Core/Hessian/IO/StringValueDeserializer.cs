using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{






/// <summary>
/// Deserializing a string valued object
/// </summary>
public class StringValueDeserializer : AbstractStringValueDeserializer {
  private Class _cl;
  private Constructor _constructor;
  
  public StringValueDeserializer(Class cl)
  {
    try {
      _cl = cl;
      _constructor = cl.GetConstructor(new Class[] { String.class });
    } catch (Exception e) {
      throw new RuntimeException(e);
    }
  }
  
    public override Class GetType()
  {
    return _cl;
  }

    protected override object Create(string value)
      {
    if (value == null)
      throw new IOException(_cl.GetName() + " expects name.");

    try {
      return _constructor.NewInstance(new Object[] { value });
    } catch (Exception e) {
      throw new HessianException(_cl.GetName() + ": value=" + value + "\n" + e,
                                 e);
    }
  }
}

}