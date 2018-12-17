using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Looks up remote objects.  The default just returns a HessianRemote object.
/// </summary>
public interface HessianRemoteResolver {
  /// <summary>
  /// Looks up a proxy object.
  /// </summary>
  public object Lookup(string type, string url)
}

}