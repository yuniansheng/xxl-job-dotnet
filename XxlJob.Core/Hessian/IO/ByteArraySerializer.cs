using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Serializing an object for known object types.
/// </summary>
public class ByteArraySerializer : AbstractSerializer
  : ObjectSerializer
{
  public static readonly ByteArraySerializer SER = new ByteArraySerializer();
  
  private ByteArraySerializer()
  {
  }

    public override ISerializer GetObjectSerializer()
  {
    return this;
  }
  
    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    byte[] data = (byte[] ) obj;
    
    if (data != null)
      out.WriteBytes(data, 0, data.length);
    else
      out.WriteNull();
  }
}

}