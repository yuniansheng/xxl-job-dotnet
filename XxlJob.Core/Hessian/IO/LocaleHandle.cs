using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{



/// <summary>
/// Handle for a locale object.
/// </summary>
public class LocaleHandle : java.io.Serializable, HessianHandle {
  private string value;

  public LocaleHandle(string locale)
  {
    this.value = locale;
  }

  private object ReadResolve()
  {
    string s = this.value;
    
    if (s == null)
      return null;
    
    int len = s.Length();
    char ch = ' ';

    int i = 0;
    for (;
         i < len && ('a' <= (ch = s.CharAt(i)) && ch <= 'z'
                     || 'A' <= ch && ch <= 'Z'
                     || '0' <= ch && ch <= '9');
         i++) {
    }

    string language = s.Substring(0, i);
    string country = null;
    string var = null;

    if (ch == '-' || ch == '_') {
      int head = ++i;
      
      for (;
           i < len && ('a' <= (ch = s.CharAt(i)) && ch <= 'z'
                       || 'A' <= ch && ch <= 'Z'
                       || '0' <= ch && ch <= '9');
           i++) {
      }
      
      country = s.Substring(head, i);
    }

    if (ch == '-' || ch == '_') {
      int head = ++i;
      
      for (;
           i < len && ('a' <= (ch = s.CharAt(i)) && ch <= 'z'
                       || 'A' <= ch && ch <= 'Z'
                       || '0' <= ch && ch <= '9');
           i++) {
      }
      
      var = s.Substring(head, i);
    }

    if (var != null)
      return new Locale(language, country, var);
    else if (country != null)
      return new Locale(language, country);
    else
      return new Locale(language);
  }
}

}