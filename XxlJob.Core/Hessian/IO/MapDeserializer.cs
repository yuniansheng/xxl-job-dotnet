using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Deserializing a JDK 1.2 Map.
/// </summary>
public class MapDeserializer : AbstractMapDeserializer {
  private Type _type;
  private Constructor<?> _ctor;
  
  public MapDeserializer(Type type)
  {
    if (type == null)
      type = HashMap.class;
    
    _type = type;

    Constructor<?>[] ctors = type.GetConstructors();
    for (int i = 0; i < ctors.length; i++) {
      if (ctors[i].GetParameterTypes().length == 0)
        _ctor = ctors[i];
    }

    if (_ctor == null) {
      try {
        _ctor = HashMap.class.GetConstructor(new Class[0]);
      } catch (Exception e) {
        throw new IllegalStateException(e);
      }
    }
  }
  
  public Type GetType()
  {
    if (_type != null)
      return _type;
    else
      return HashMap.class;
  }

  public object ReadMap(AbstractHessianInput in)
      {
    Map map;
    
    if (_type == null)
      map = new HashMap();
    else if (_type.Equals(Map.class))
      map = new HashMap();
    else if (_type.Equals(SortedMap.class))
      map = new TreeMap();
    else {
      try {
        map = (Map) _ctor.NewInstance();
      } catch (Exception e) {
        throw new IOExceptionWrapper(e);
      }
    }

    in.AddRef(map);

    while (! in.IsEnd()) {
      map.Put(in.ReadObject(), in.ReadObject());
    }

    in.ReadEnd();

    return map;
  }

    public override object ReadObject(AbstractHessianInput in,
                           object[] fields)
      {
    string[] fieldNames = (string[] ) fields;
    Map<Object,Object> map = CreateMap();
      
    int ref = in.AddRef(map);

    for (int i = 0; i < fieldNames.length; i++) {
      string name = fieldNames[i];

      map.Put(name, in.ReadObject());
    }

    return map;
  }

  private Map CreateMap()
      {
    
    if (_type == null)
      return new HashMap();
    else if (_type.Equals(Map.class))
      return new HashMap();
    else if (_type.Equals(SortedMap.class))
      return new TreeMap();
    else {
      try {
        return (Map) _ctor.NewInstance();
      } catch (Exception e) {
        throw new IOExceptionWrapper(e);
      }
    }
  }
}

}