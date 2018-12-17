using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Exception during field reading.
/// </summary>
public class HessianFieldException : HessianProtocolException {
  /// <summary>
  /// Zero-arg constructor.
  /// </summary>
  public HessianFieldException()
  {
  }
  
  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianFieldException(string message)
  {
    Super(message);
  }
  
  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianFieldException(string message, Throwable cause)
  {
    Super(message, cause);
  }
  
  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianFieldException(Throwable cause)
  {
    Super(cause);
  }
}

}