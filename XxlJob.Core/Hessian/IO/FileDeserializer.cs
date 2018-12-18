using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{






/// <summary>
/// Deserializing a File
/// </summary>
public class FileDeserializer : AbstractStringValueDeserializer {
    public override Class GetType()
  {
    return File.class;
  }

    protected override object Create(string value)
  {
    return new File(value);
  }
}

}