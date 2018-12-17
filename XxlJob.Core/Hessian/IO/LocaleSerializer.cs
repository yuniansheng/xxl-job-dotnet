using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a locale.
/// </summary>
public class LocaleSerializer : AbstractSerializer {
  private static LocaleSerializer SERIALIZER = new LocaleSerializer();

  public static LocaleSerializer Create()
  {
    return SERIALIZER;
  }
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (obj == null)
      out.WriteNull();
    else {
      Locale locale = (Locale) obj;

      out.WriteObject(new LocaleHandle(locale.ToString()));
    }
  }
}

}