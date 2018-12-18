using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Handle for a calendar object.
/// </summary>
public class CalendarHandle : java.io.Serializable, HessianHandle {
  private Class type;
  private Date date;

  public CalendarHandle()
  {
  }
  
  public CalendarHandle(Class type, long time)
  {
    if (! GregorianCalendar.class.Equals(type))
      this.type = type;
    
    this.date = new Date(time);
  }

  private object ReadResolve()
  {
    try {
      Calendar cal;
      
      if (this.type != null)
        cal = (Calendar) this.type.NewInstance();
      else
        cal = new GregorianCalendar();
      
      cal.SetTimeInMillis(this.date.GetTime());

      return cal;
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new RuntimeException(e);
    }
  }
}

}