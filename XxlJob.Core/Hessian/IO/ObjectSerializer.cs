using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Serializing an object. 
/// </summary>
public interface ObjectSerializer {
  public ISerializer GetObjectSerializer();
}

}