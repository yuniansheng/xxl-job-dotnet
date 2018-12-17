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
public class BeanSerializer : AbstractSerializer {
  private static readonly Logger log
    = Logger.GetLogger(BeanSerializer.class.GetName());
  
  private static readonly object[] NULL_ARGS = new Object[0];
  private Method[] _methods;
  private string[] _names;

  private object _writeReplaceFactory;
  private Method _writeReplace;
  
  public BeanSerializer(Type cl, ClassLoader loader)
  {
    IntrospectWriteReplace(cl, loader);

    ArrayList<Method> primitiveMethods = new ArrayList<Method>();
    ArrayList compoundMethods = new ArrayList();
    
    for (; cl != null; cl = cl.GetSuperclass()) {
      Method[] methods = cl.GetDeclaredMethods();
      
      for (int i = 0; i < methods.length; i++) {
        Method method = methods[i];

        if (Modifier.IsStatic(method.GetModifiers()))
          continue;

        if (method.GetParameterTypes().length != 0)
          continue;

        string name = method.GetName();

        if (! name.StartsWith("get"))
          continue;

        Class type = method.GetReturnType();

        if (type.Equals(void.class))
          continue;

        if (FindSetter(methods, name, type) == null)
          continue;

        // XXX: could parameterize the handler to only deal with public
        method.SetAccessible(true);

        if (type.IsPrimitive()
            || type.GetName().StartsWith("java.lang.")
            && ! type.Equals(Object.class))
          primitiveMethods.Add(method);
        else
          compoundMethods.Add(method);
      }
    }

    ArrayList methodList = new ArrayList();
    methodList.AddAll(primitiveMethods);
    methodList.AddAll(compoundMethods);

    Collections.Sort(methodList, new MethodNameCmp());

    _methods = new Method[methodList.Size()];
    methodList.ToArray(_methods);

    _names = new string[_methods.length];
    
    for (int i = 0; i < _methods.length; i++) {
      string name = _methods[i].GetName();

      name = name.Substring(3);

      int j = 0;
      for (; j < name.Length() && Character.IsUpperCase(name.CharAt(j)); j++) {
      }

      if (j == 1)
        name = name.Substring(0, j).ToLowerCase(Locale.ENGLISH) + name.Substring(j);
      else if (j > 1)
        name = name.Substring(0, j - 1).ToLowerCase(Locale.ENGLISH) + name.Substring(j - 1);

      _names[i] = name;
    }
  }

  private void IntrospectWriteReplace(Class cl, ClassLoader loader)
  {
    try {
      string className = cl.GetName() + "HessianSerializer";

      Class serializerClass = Class.ForName(className, false, loader);

      object serializerobject = serializerClass.NewInstance();

      Method writeReplace = GetWriteReplace(serializerClass, cl);

      if (writeReplace != null) {
        _writeReplaceFactory = serializerObject;
        _writeReplace = writeReplace;

        return;
      }
    } catch (ClassNotFoundException e) {
    } catch (Exception e) {
      log.Log(Level.FINER, e.ToString(), e);
    }
      
    _writeReplace = GetWriteReplace(cl);
  }

  /// <summary>
  /// Returns the writeReplace method
  /// </summary>
  protected Method GetWriteReplace(Class cl)
  {
    for (; cl != null; cl = cl.GetSuperclass()) {
      Method[] methods = cl.GetDeclaredMethods();
      
      for (int i = 0; i < methods.length; i++) {
        Method method = methods[i];

        if (method.GetName().Equals("writeReplace") &&
            method.GetParameterTypes().length == 0)
          return method;
      }
    }

    return null;
  }

  /// <summary>
  /// Returns the writeReplace method
  /// </summary>
  protected Method GetWriteReplace(Class cl, Class param)
  {
    for (; cl != null; cl = cl.GetSuperclass()) {
      for (Method method : cl.GetDeclaredMethods()) {
        if (method.GetName().Equals("writeReplace")
            && method.GetParameterTypes().length == 1
            && param.Equals(method.GetParameterTypes()[0]))
          return method;
      }
    }

    return null;
  }
  
  public void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (out.AddRef(obj))
      return;
    
    Class cl = obj.GetClass();
    
    try {
      if (_writeReplace != null) {
        object repl;

        if (_writeReplaceFactory != null)
          repl = _writeReplace.Invoke(_writeReplaceFactory, obj);
        else
          repl = _writeReplace.Invoke(obj);

        // out.RemoveRef(obj);

        out.WriteObject(repl);

        out.ReplaceRef(repl, obj);

        return;
      }
    } catch (Exception e) {
      log.Log(Level.FINER, e.ToString(), e);
    }

    int ref = out.WriteObjectBegin(cl.GetName());

    if (ref < -1) {
      // Hessian 1.1 uses a map
      
      for (int i = 0; i < _methods.length; i++) {
        Method method = _methods[i];
        object value = null;

        try {
          value = _methods[i].Invoke(obj, (object[] ) null);
        } catch (Exception e) {
          log.Log(Level.FINE, e.ToString(), e);
        }

        out.WriteString(_names[i]);

        out.WriteObject(value);
      }
      
      out.WriteMapEnd();
    }
    else {
      if (ref == -1) {
        out.WriteInt(_names.length);

        for (int i = 0; i < _names.length; i++)
          out.WriteString(_names[i]);

        out.WriteObjectBegin(cl.GetName());
      }

      for (int i = 0; i < _methods.length; i++) {
        Method method = _methods[i];
        object value = null;

        try {
          value = _methods[i].Invoke(obj, (object[] ) null);
        } catch (Exception e) {
          log.Log(Level.FINER, e.ToString(), e);
        }

        out.WriteObject(value);
      }
    }
  }

  /// <summary>
  /// Finds any matching setter.
  /// </summary>
  private Method FindSetter(Method[] methods, string getterName, Class arg)
  {
    string setterName = "set" + getterName.Substring(3);
    
    for (int i = 0; i < methods.length; i++) {
      Method method = methods[i];

      if (! method.GetName().Equals(setterName))
        continue;
      
      if (! method.GetReturnType().Equals(void.class))
        continue;

      Class[] params = method.GetParameterTypes();

      if (params.length == 1 && params[0].Equals(arg))
        return method;
    }

    return null;
  }

  static class MethodNameCmp : Comparator<Method> {
    public int Compare(Method a, Method b)
    {
      return a.GetName().CompareTo(b.GetName());
    }
  }
}

}