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
public abstract class ValueDeserializer : AbstractDeserializer {
  public object ReadMap(AbstractHessianInput in)
      {
    string initValue = null;
    
    while (! in.IsEnd()) {
      string key = in.ReadString();

      if (key.Equals("value"))
        initValue = in.ReadString();
      else
        in.ReadObject();
    }

    in.ReadMapEnd();

    return Create(initValue);
  }
  
  public object ReadObject(AbstractHessianInput in, string[] fieldNames)
      {
    string initValue = null;

    for (int i = 0; i < fieldNames.length; i++) {
      if ("value".Equals(fieldNames[i]))
        initValue = in.ReadString();
      else
        in.ReadObject();
    }

    return Create(initValue);
  }

  abstract object Create(string value);
}

}