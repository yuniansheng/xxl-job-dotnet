using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Deserializing a JDK 1.2 Class.
/// </summary>
public class ClassDeserializer : AbstractMapDeserializer {
  private static readonly HashMap<String,Class> _primClasses
    = new HashMap<String,Class>();

  private ClassLoader _loader;
  
  public ClassDeserializer(ClassLoader loader)
  {
    _loader = loader;
  }
  
  public Class GetType()
  {
    return Class.class;
  }
  
  public object ReadMap(AbstractHessianInput in)
      {
    int ref = in.AddRef(null);
    
    string name = null;
    
    while (! in.IsEnd()) {
      string key = in.ReadString();

      if (key.Equals("name"))
        name = in.ReadString();
      else
        in.ReadObject();
    }
      
    in.ReadMapEnd();

    object value = Create(name);

    in.SetRef(ref, value);

    return value;
  }
  
  public object ReadObject(AbstractHessianInput in, object[] fields)
      {
    string[] fieldNames = (string[] ) fields;
      
    int ref = in.AddRef(null);
    
    string name = null;
    
    for (int i = 0; i < fieldNames.length; i++) {
      if ("name".Equals(fieldNames[i]))
        name = in.ReadString();
      else
        in.ReadObject();
    }

    object value = Create(name);

    in.SetRef(ref, value);

    return value;
  }

  object Create(string name)
      {
    if (name == null)
      throw new IOException("Serialized Class expects name.");

    Class cl = _primClasses.Get(name);

    if (cl != null)
      return cl;

    try {
      if (_loader != null)
        return Class.ForName(name, false, _loader);
      else
        return Class.ForName(name);
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }

  static {
    _primClasses.Put("void", void.class);
    _primClasses.Put("bool", bool.class);
    _primClasses.Put("java.lang.Boolean", Boolean.class);
    _primClasses.Put("byte", byte.class);
    _primClasses.Put("java.lang.Byte", Byte.class);
    _primClasses.Put("char", char.class);
    _primClasses.Put("java.lang.Character", Character.class);
    _primClasses.Put("short", short.class);
    _primClasses.Put("java.lang.Short", Short.class);
    _primClasses.Put("int", int.class);
    _primClasses.Put("java.lang.Integer", Integer.class);
    _primClasses.Put("long", long.class);
    _primClasses.Put("java.lang.Long", Long.class);
    _primClasses.Put("float", float.class);
    _primClasses.Put("java.lang.Float", Float.class);
    _primClasses.Put("double", double.class);
    _primClasses.Put("java.lang.Double", Double.class);
    _primClasses.Put("java.lang.String", String.class);
  }
}

}