using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Deserializing a BigDecimal
/// </summary>
public class BigDecimalDeserializer : AbstractStringValueDeserializer {
    public override Type GetType()
  {
    return BigDecimal.class;
  }

    protected override object Create(string value)
  {
    return new BigDecimal(value);
  }
}

}