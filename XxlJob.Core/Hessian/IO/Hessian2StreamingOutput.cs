using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Output stream for Hessian 2 streaming requests.
/// </summary>
public class Hessian2StreamingOutput
{
  private Hessian2Output _out;
  
  /// <summary>
  /// Creates a new Hessian output stream, initialized with an
  /// underlying output stream.
  ///
  /// <param name="os">the underlying output stream.</param>
  /// </summary>
  public Hessian2StreamingOutput(OutputStream os)
  {
    _out = new Hessian2Output(os);
  }
  
  public Hessian2StreamingOutput(Hessian2Output out)
  {
    _out = out;
  }

  public Hessian2Output GetHessian2Output()
  {
    return _out;
  }
  
  public void SetCloseStreamOnClose(bool isClose)
  {
    _out.SetCloseStreamOnClose(isClose);
  }
  
  public bool IsCloseStreamOnClose()
  {
    return _out.IsCloseStreamOnClose();
  }

  /// <summary>
  /// Writes any object to the output stream.
  /// </summary>
  public void WriteObject(object object)
      {
    _out.WriteStreamingObject(object);
  }

  /// <summary>
  /// Flushes the output.
  /// </summary>
  public void Flush()
      {
    _out.Flush();
  }

  /// <summary>
  /// Close the output.
  /// </summary>
  public void Close()
      {
    _out.Close();
  }
}

}