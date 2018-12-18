using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a locale.
/// </summary>
public class InetAddressSerializer : AbstractSerializer {
  private static InetAddressSerializer SERIALIZER = new InetAddressSerializer();

  public static InetAddressSerializer Create()
  {
    return SERIALIZER;
  }
  
    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (obj == null)
      out.WriteNull();
    else {
      InetAddress addr = (InetAddress) obj;
      out.WriteObject(new InetAddressHandle(addr.GetHostName(),
                                            addr.GetAddress()));
    }
  }
}

}