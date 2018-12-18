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
public class HessianServiceException : Exception {
  private string code;
  private object detail;

  /// <summary>
  /// Zero-arg constructor.
  /// </summary>
  public HessianServiceException()
  {
  }

  /// <summary>
  /// Create the exception.
  /// </summary>
  public HessianServiceException(string message, string code, object detail)
  {
    Super(message);
    this.code = code;
    this.detail = detail;
  }

  /// <summary>
  /// Returns the code.
  /// </summary>
  public string GetCode()
  {
    return code;
  }

  /// <summary>
  /// Returns the detail.
  /// </summary>
  public object GetDetail()
  {
    return detail;
  }
}

}