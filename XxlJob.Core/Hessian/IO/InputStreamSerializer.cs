using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a stream object.
/// </summary>
public class InputStreamSerializer : AbstractSerializer {
  public InputStreamSerializer()
  {
  }
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    InputStream is = (InputStream) obj;

    if (is == null)
      out.WriteNull();
    else {
      out.WriteByteStream(is);
    }
  }
}

}