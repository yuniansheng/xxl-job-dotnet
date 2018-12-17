using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Factory class for wrapping and unwrapping hessian streams.
/// </summary>
public abstract class HessianEnvelope {
  /// <summary>
  /// Wrap the Hessian output stream in an envelope.
  /// </summary>
  public abstract Hessian2Output Wrap(Hessian2Output out);

  /// <summary>
  /// Unwrap the Hessian input stream with this envelope.  It is an
  /// error if the actual envelope does not match the expected envelope
  /// class.
  /// </summary>
  public abstract Hessian2Input Unwrap(Hessian2Input in);

  /// <summary>
  /// Unwrap the envelope after having read the envelope code ('E') and
  /// the envelope method.  Called by the EnvelopeFactory for dynamic
  /// reading of the envelopes.
  /// </summary>
  public abstract Hessian2Input UnwrapHeaders(Hessian2Input in);
}

}