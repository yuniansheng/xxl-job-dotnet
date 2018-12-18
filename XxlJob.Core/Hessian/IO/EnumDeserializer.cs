using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Deserializing an enum valued object
/// </summary>
public class EnumDeserializer : AbstractDeserializer {
  private Class _enumType;
  private Method _valueOf;
  
  public EnumDeserializer(Class cl)
  {
    // hessian/33b[34], hessian/3bb[78]
    if (cl.IsEnum())
      _enumType = cl;
    else if (cl.GetSuperclass().IsEnum())
      _enumType = cl.GetSuperclass();
    else
      throw new RuntimeException("Class " + cl.GetName() + " is not an enum");

    try {
      _valueOf = _enumType.GetMethod("valueOf",
                             new Class[] { Class.class, String.class });
    } catch (Exception e) {
      throw new RuntimeException(e);
    }
  }
  
  public Class GetType()
  {
    return _enumType;
  }
  
  public object ReadMap(AbstractHessianInput in)
      {
    string name = null;
    
    while (! in.IsEnd()) {
      string key = in.ReadString();

      if (key.Equals("name"))
        name = in.ReadString();
      else
        in.ReadObject();
    }

    in.ReadMapEnd();

    object obj = Create(name);
    
    in.AddRef(obj);

    return obj;
  }
  
    public override object ReadObject(AbstractHessianInput in, object[] fields)
      {
    string[] fieldNames = (string[] ) fields;
    string name = null;

    for (int i = 0; i < fieldNames.length; i++) {
      if ("name".Equals(fieldNames[i]))
        name = in.ReadString();
      else
        in.ReadObject();
    }

    object obj = Create(name);

    in.AddRef(obj);

    return obj;
  }

  private object Create(string name)
      {
    if (name == null)
      throw new IOException(_enumType.GetName() + " expects name.");

    try {
      return _valueOf.Invoke(null, _enumType, name);
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }
}

}