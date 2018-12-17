using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Handle for an InetAddress object.
/// </summary>
public class InetAddressHandle : java.io.Serializable, HessianHandle
{
  private static readonly Logger log = Logger.GetLogger(InetAddressHandle.class.GetName());
  
  private string hostName;
  private byte[] address;

  public InetAddressHandle(string hostName, byte[] address)
  {
    this.hostName = hostName;
    this.address = address;
  }

  private object ReadResolve()
  {
    try {
      return InetAddress.GetByAddress(this.hostName, this.address);
    } catch (Exception e) {
      log.Log(Level.FINE, e.ToString(), e);
      
      return null;
    }
  }
}

}