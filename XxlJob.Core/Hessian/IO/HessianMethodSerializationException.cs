using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Exception for faults when the fault doesn't return a java exception.
/// This exception is required for MicroHessianInput.
/// </summary>
public class HessianMethodSerializationException : HessianException {
  /// <summary>
  /// Zero-arg constructor.
  /// </summary>
  public HessianMethodSerializationException()
  {
  }
  
  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianMethodSerializationException(string message)
  {
    Super(message);
  }
  
  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianMethodSerializationException(string message, Throwable cause)
  {
    Super(message, cause);
  }
  
  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianMethodSerializationException(Throwable cause)
  {
    Super(cause);
  }
}

}