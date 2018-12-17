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
public class WriteReplaceSerializer : AbstractSerializer
{
  private static readonly Logger log
    = Logger.GetLogger(WriteReplaceSerializer.class.GetName());

  private object _writeReplaceFactory;
  private Method _writeReplace;
  private ISerializer _baseSerializer;
  
  public WriteReplaceSerializer(Type cl,
                                ClassLoader loader,
                                ISerializer baseSerializer)
  {
    IntrospectWriteReplace(cl, loader);
    
    _baseSerializer = baseSerializer;
  }

  private void IntrospectWriteReplace(Type cl, ClassLoader loader)
  {
    try {
      string className = cl.GetName() + "HessianSerializer";

      Type serializerClass = Class.ForName(className, false, loader);

      object serializerobject = serializerClass.NewInstance();

      Method writeReplace = GetWriteReplace(serializerClass, cl);

      if (writeReplace != null) {
        _writeReplaceFactory = serializerObject;
        _writeReplace = writeReplace;
      }
    } catch (ClassNotFoundException e) {
    } catch (Exception e) {
      log.Log(Level.FINER, e.ToString(), e);
    }
      
    _writeReplace = GetWriteReplace(cl);
    if (_writeReplace != null)
      _writeReplace.SetAccessible(true);
  }

  /// <summary>
  /// Returns the writeReplace method
  /// </summary>
  protected static Method GetWriteReplace(Type cl, Type param)
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

  /// <summary>
  /// Returns the writeReplace method
  /// </summary>
  protected static Method GetWriteReplace(Type cl)
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

    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    int ref = out.GetRef(obj);
    
    if (ref >= 0) {
      out.WriteRef(ref);
      
      return;
    }
    
    try {
      object repl;

      repl = WriteReplace(obj);

      if (obj == repl) {
        if (log.IsLoggable(Level.FINE)) { 
          log.Fine(this + ": Hessian writeReplace error.  The writeReplace method (" + _writeReplace + ") must not return the same object: " + obj);
        }
        
        _baseSerializer.WriteObject(obj, out);

        return;
      }

      out.WriteObject(repl);

      out.ReplaceRef(repl, obj);
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new RuntimeException(e);
    }
  }
  
    protected override object WriteReplace(object obj)
  {
    try {
      if (_writeReplaceFactory != null)
        return _writeReplace.Invoke(_writeReplaceFactory, obj);
      else
        return _writeReplace.Invoke(obj);
    } catch (RuntimeException e) {
      throw e;
    } catch (InvocationTargetException e) {
      throw new RuntimeException(e.GetCause());
    } catch (Exception e) {
      throw new RuntimeException(e);
    }
  }
}

}