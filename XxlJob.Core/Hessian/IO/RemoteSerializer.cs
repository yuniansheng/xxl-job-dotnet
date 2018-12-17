using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Serializing a remote object.
/// </summary>
public class RemoteSerializer : AbstractSerializer {
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    HessianRemoteobject remoteobject = (HessianRemoteObject) obj;

    out.WriteObject(new HessianRemote(remoteObject.GetHessianType(),
                                      remoteObject.GetHessianURL()));
  }
}

}