using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Exception wrapper for IO.
/// </summary>
public class IOExceptionWrapper : IOException {
  private Throwable _cause;
  
  public IOExceptionWrapper(Throwable cause)
  {
    Super(cause.ToString());

    _cause = cause;
  }
  
  public IOExceptionWrapper(string msg, Throwable cause)
  {
    Super(msg);

    _cause = cause;
  }

  public Throwable GetCause()
  {
    return _cause;
  }
}

}