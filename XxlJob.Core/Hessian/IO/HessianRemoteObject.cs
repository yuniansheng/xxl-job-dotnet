using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{

/// <summary>
/// Interface for any hessian remote object.
/// </summary>
public interface HessianRemoteobject {
  public string GetHessianType();
  public string GetHessianURL();
}

}