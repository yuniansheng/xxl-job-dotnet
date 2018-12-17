using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Serializing a calendar.
/// </summary>
public class CalendarSerializer : AbstractSerializer {
  public static readonly ISerializer SER = new CalendarSerializer();
  
  /// <summary>
  /// java.util.Calendar serializes to com.caucho.hessian.io.CalendarHandle
  /// </summary>
    public override object WriteReplace(object obj)
  {
    Calendar cal = (Calendar) obj;

    return new CalendarHandle(cal.GetClass(), cal.GetTimeInMillis());
  }
}

}