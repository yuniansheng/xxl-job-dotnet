using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Deserializing a JDK 1.4 StackTraceElement
/// </summary>
public class StackTraceElementDeserializer : JavaDeserializer {
  public StackTraceElementDeserializer()
  {
    Super(StackTraceElement.class);
  }

    protected override object Instantiate()
  {
    return new StackTraceElement("", "", "", 0);
  }
}

}