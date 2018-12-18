using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{









/// <summary>
/// Input stream for Hessian requests, deserializing objects using the
/// java.io.Serialization protocol.
///
/// <para/>HessianSerializerInput is unbuffered, so any client needs to provide
/// its own buffering.
///
/// <h3>Serialization</h3>
///
/// <code>
/// InputStream is = new FileInputStream("test.xml");
/// HessianOutput in = new HessianSerializerOutput(is);
///
/// object obj = in.ReadObject();
/// is.Close();
/// </code>
///
/// <h3>Parsing a Hessian reply</h3>
///
/// <code>
/// InputStream is = ...; // from http connection
/// HessianInput in = new HessianSerializerInput(is);
/// string value;
///
/// in.StartReply();         // read reply header
/// value = in.ReadString(); // read string value
/// in.CompleteReply();      // read reply footer
/// </code>
/// </summary>
public class HessianSerializerInput : Hessian2Input {
  /// <summary>
  /// Creates a new Hessian input stream, initialized with an
  /// underlying input stream.
  ///
  /// <param name="is">the underlying input stream.</param>
  /// </summary>
  public HessianSerializerInput(InputStream is)
  {
    Super(is);
  }

  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianSerializerInput()
  {
    Super(null);
  }

  /// <summary>
  /// Reads an object from the input stream.  cl is known not to be
  /// a Map.
  /// </summary>
  protected object ReadObjectImpl(Class cl)
      {
    try {
      object obj = cl.NewInstance();

      if (_refs == null)
        _refs = new ArrayList();
      _refs.Add(obj);

      HashMap fieldMap = GetFieldMap(cl);

      int code = Read();
      for (; code >= 0 && code != 'z'; code = Read()) {
        Unread();
        
        object key = ReadObject();
        
        Field field = (Field) fieldMap.Get(key);

        if (field != null) {
          object value = ReadObject(field.GetType());
          field.Set(obj, value);
        }
        else {
          object value = ReadObject();
        }
      }
      
      if (code != 'z')
        throw Expect("map", code);

      // if there's a readResolve method, call it
      try {
        Method method = cl.GetMethod("readResolve", new Class[0]);
        return method.Invoke(obj, new Object[0]);
      } catch (Exception e) {
      }

      return obj;
    } catch (IOException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }

  /// <summary>
  /// Creates a map of the classes fields.
  /// </summary>
  protected HashMap GetFieldMap(Class cl)
  {
    HashMap fieldMap = new HashMap();
    
    for (; cl != null; cl = cl.GetSuperclass()) {
      Field[] fields = cl.GetDeclaredFields();
      for (int i = 0; i < fields.length; i++) {
        Field field = fields[i];

        if (Modifier.IsTransient(field.GetModifiers()) ||
            Modifier.IsStatic(field.GetModifiers()))
          continue;

        // XXX: could parameterize the handler to only deal with public
        field.SetAccessible(true);

        fieldMap.Put(field.GetName(), field);
      }
    }

    return fieldMap;
  }
}

}