using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{

/// <summary>
/// Encapsulates a remote address when no stub is available, e.g. for
/// Java MicroEdition.
/// </summary>
public class HessianRemote : java.io.Serializable {
  private string type;
  private string url;

  /// <summary>
  /// Creates a new Hessian remote object.
  ///
  /// <param name="type">the remote stub interface</param>
  /// <param name="url">the remote url</param>
  /// </summary>
  public HessianRemote(string type, string url)
  {
    this.type = type;
    this.url = url;
  }

  /// <summary>
  /// Creates an uninitialized Hessian remote.
  /// </summary>
  public HessianRemote()
  {
  }

  /// <summary>
  /// Returns the remote api class name.
  /// </summary>
  public string GetType()
  {
    return type;
  }

  /// <summary>
  /// Returns the remote URL.
  /// </summary>
  public string GetURL()
  {
    return url;
  }

  /// <summary>
  /// Sets the remote URL.
  /// </summary>
  public void SetURL(string url)
  {
    this.url = url;
  }

  /// <summary>
  /// Defines the hashcode.
  /// </summary>
  public int HashCode()
  {
    return url.HashCode();
  }

  /// <summary>
  /// Defines equality
  /// </summary>
  public bool Equals(object obj)
  {
    if (! (obj instanceof HessianRemote))
      return false;

    HessianRemote remote = (HessianRemote) obj;

    return url.Equals(remote.url);
  }

  /// <summary>
  /// Readable version of the remote.
  /// </summary>
  public string ToString()
  {
    return "HessianRemote[" + url + "]";
  }
}

}