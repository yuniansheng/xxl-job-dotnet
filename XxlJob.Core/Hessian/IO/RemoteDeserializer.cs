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
public class RemoteDeserializer :  JavaDeserializer {
  private static readonly Logger log
    = Logger.GetLogger(RemoteDeserializer.class.GetName());
  
  public static readonly IDeserializer DESER = new RemoteDeserializer();
  
  public RemoteDeserializer()
  {
    Super(HessianRemote.class);
  }

    public override bool IsReadResolve()
  {
    return true;
  }

    protected override object Resolve(AbstractHessianInput in, object obj)
  {
    HessianRemote remote = (HessianRemote) obj;
    HessianRemoteResolver resolver = in.GetRemoteResolver();

    if (resolver != null) {
      object proxy = resolver.Lookup(remote.GetType(), remote.GetURL());

      return proxy;
    }
    else
      return remote;
  }
}

}