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
public class InputStreamDeserializer : AbstractDeserializer {
  public static readonly InputStreamDeserializer DESER
    = new InputStreamDeserializer();
  
  public InputStreamDeserializer()
  {
  }
  
  public object ReadObject(AbstractHessianInput in)
      {
    return in.ReadInputStream();
  }
}

}