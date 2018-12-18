using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{








/// <summary>
/// Serializing an object for known object types.
/// </summary>
public class BeanDeserializer : AbstractMapDeserializer {
  private Class _type;
  private HashMap _methodMap;
  private Method _readResolve;
  private Constructor _constructor;
  private object[] _constructorArgs;
  
  public BeanDeserializer(Class cl)
  {
    _type = cl;
    _methodMap = GetMethodMap(cl);

    _readResolve = GetReadResolve(cl);

    Constructor[] constructors = cl.GetConstructors();
    int bestLength = Integer.MAX_VALUE;
    
    for (int i = 0; i < constructors.length; i++) {
      if (constructors[i].GetParameterTypes().length < bestLength) {
        _constructor = constructors[i];
        bestLength = _constructor.GetParameterTypes().length;
      }
    }

    if (_constructor != null) {
      _constructor.SetAccessible(true);
      Class[] params = _constructor.GetParameterTypes();
      _constructorArgs = new Object[params.length];
      for (int i = 0; i < params.length; i++) {
        _constructorArgs[i] = GetParamArg(params[i]);
      }
    }
  }

  public Class GetType()
  {
    return _type;
  }
    
  public object ReadMap(AbstractHessianInput in)
      {
    try {
      object obj = Instantiate();

      return ReadMap(in, obj);
    } catch (IOException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }
    
  public object ReadMap(AbstractHessianInput in, object obj)
      {
    try {
      int ref = in.AddRef(obj);

      while (! in.IsEnd()) {
        object key = in.ReadObject();
        
        Method method = (Method) _methodMap.Get(key);

        if (method != null) {
          object value = in.ReadObject(method.GetParameterTypes()[0]);

          method.Invoke(obj, new Object[] {value });
        }
        else {
          object value = in.ReadObject();
        }
      }
      
      in.ReadMapEnd();

      object resolve = Resolve(obj);

      if (obj != resolve)
        in.SetRef(ref, resolve);

      return resolve;
    } catch (IOException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }

  private object Resolve(object obj)
  {
    // if there's a readResolve method, call it
    try {
      if (_readResolve != null)
        return _readResolve.Invoke(obj, new Object[0]);
    } catch (Exception e) {
    }

    return obj;
  }

  protected object Instantiate()
  {
    return _constructor.NewInstance(_constructorArgs);
  }

  /// <summary>
  /// Returns the readResolve method
  /// </summary>
  protected Method GetReadResolve(Class cl)
  {
    for (; cl != null; cl = cl.GetSuperclass()) {
      Method[] methods = cl.GetDeclaredMethods();
      
      for (int i = 0; i < methods.length; i++) {
        Method method = methods[i];

        if (method.GetName().Equals("readResolve") &&
            method.GetParameterTypes().length == 0)
          return method;
      }
    }

    return null;
  }

  /// <summary>
  /// Creates a map of the classes fields.
  /// </summary>
  protected HashMap GetMethodMap(Class cl)
  {
    HashMap methodMap = new HashMap();
    
    for (; cl != null; cl = cl.GetSuperclass()) {
      Method[] methods = cl.GetDeclaredMethods();
      
      for (int i = 0; i < methods.length; i++) {
        Method method = methods[i];

        if (Modifier.IsStatic(method.GetModifiers()))
          continue;

        string name = method.GetName();

        if (! name.StartsWith("set"))
          continue;

        Class[] paramTypes = method.GetParameterTypes();
        if (paramTypes.length != 1)
          continue;

        if (! method.GetReturnType().Equals(void.class))
          continue;

        if (FindGetter(methods, name, paramTypes[0]) == null)
          continue;

        // XXX: could parameterize the handler to only deal with public
        try {
          method.SetAccessible(true);
        } catch (Throwable e) {
          e.PrintStackTrace();
        }
    
        name = name.Substring(3);

        int j = 0;
        for (; j < name.Length() && Character.IsUpperCase(name.CharAt(j)); j++) {
        }

        if (j == 1)
          name = name.Substring(0, j).ToLowerCase(Locale.ENGLISH) + name.Substring(j);
        else if (j > 1)
          name = name.Substring(0, j - 1).ToLowerCase(Locale.ENGLISH) + name.Substring(j - 1);


        methodMap.Put(name, method);
      }
    }

    return methodMap;
  }

  /// <summary>
  /// Finds any matching setter.
  /// </summary>
  private Method FindGetter(Method[] methods, string setterName, Class arg)
  {
    string getterName = "get" + setterName.Substring(3);
    
    for (int i = 0; i < methods.length; i++) {
      Method method = methods[i];

      if (! method.GetName().Equals(getterName))
        continue;
      
      if (! method.GetReturnType().Equals(arg))
        continue;

      Class[] params = method.GetParameterTypes();

      if (params.length == 0)
        return method;
    }

    return null;
  }

  /// <summary>
  /// Creates a map of the classes fields.
  /// </summary>
  protected static object GetParamArg(Class cl)
  {
    if (! cl.IsPrimitive())
      return null;
    else if (bool.class.Equals(cl))
      return Boolean.FALSE;
    else if (byte.class.Equals(cl))
      return Byte.ValueOf((byte) 0);
    else if (short.class.Equals(cl))
      return Short.ValueOf((short) 0);
    else if (char.class.Equals(cl))
      return Character.ValueOf((char) 0);
    else if (int.class.Equals(cl))
      return Integer.ValueOf(0);
    else if (long.class.Equals(cl))
      return Long.ValueOf(0);
    else if (float.class.Equals(cl))
      return Double.ValueOf(0);
    else if (double.class.Equals(cl))
      return Double.ValueOf(0);
    else
      throw new NotSupportedException();
  }
}

}