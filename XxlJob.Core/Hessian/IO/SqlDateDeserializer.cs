using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{






/// <summary>
/// Deserializing a string valued object
/// </summary>
public class SqlDateDeserializer : AbstractDeserializer {
  private Class _cl;
  private Constructor _constructor;
  
  public SqlDateDeserializer(Class cl)
  {
    try {
      _cl = cl;
      _constructor = cl.GetConstructor(new Class[] { long.class });
    } catch (NoSuchMethodException e) {
      throw new HessianException(e);
    }
  }
  
  public Class GetType()
  {
    return _cl;
  }
  
  public object ReadMap(AbstractHessianInput in)
      {
    int ref = in.AddRef(null);
    
    long initValue = Long.MIN_VALUE;
    
    while (! in.IsEnd()) {
      string key = in.ReadString();

      if (key.Equals("value"))
        initValue = in.ReadUTCDate();
      else
        in.ReadString();
    }

    in.ReadMapEnd();

    object value = Create(initValue);

    in.SetRef(ref, value);

    return value;
  }
  
  public object ReadObject(AbstractHessianInput in,
                           object[] fields)
      {
    string[] fieldNames = (string[] ) fields;
    
    int ref = in.AddRef(null);
    
    long initValue = Long.MIN_VALUE;

    for (int i = 0; i < fieldNames.length; i++) {
      string key = fieldNames[i];

      if (key.Equals("value"))
        initValue = in.ReadUTCDate();
      else
        in.ReadObject();
    }

    object value = Create(initValue);

    in.SetRef(ref, value);

    return value;
  }

  private object Create(long initValue)
      {
    if (initValue == Long.MIN_VALUE)
      throw new IOException(_cl.GetName() + " expects name.");

    try {
      return _constructor.NewInstance(new Object[] { new Long(initValue) });
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }
}

}