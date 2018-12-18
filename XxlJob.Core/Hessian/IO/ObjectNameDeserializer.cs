using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Deserializing an ObjectName
/// </summary>
public class ObjectNameDeserializer : AbstractStringValueDeserializer {
    public override Type GetType()
  {
    return ObjectName.class;
  }

    protected override object Create(string value)
  {
    try {
      return new ObjectName(value);
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new HessianException(e);
    }
  }
}

}