using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{







/// <summary>
/// Output stream for Hessian requests.
///
/// <para/>HessianOutput is unbuffered, so any client needs to provide
/// its own buffering.
///
/// <h3>Serialization</h3>
///
/// <code>
/// OutputStream os = new FileOutputStream("test.xml");
/// HessianOutput out = new HessianSerializerOutput(os);
///
/// out.WriteObject(obj);
/// os.Close();
/// </code>
///
/// <h3>Writing an RPC Call</h3>
///
/// <code>
/// OutputStream os = ...; // from http connection
/// HessianOutput out = new HessianSerializerOutput(os);
/// string value;
///
/// out.StartCall("hello");  // start hello call
/// out.WriteString("arg1"); // write a string argument
/// out.CompleteCall();      // complete the call
/// </code>
/// </summary>
public class HessianSerializerOutput : Hessian2Output {
  /// <summary>
  /// Creates a new Hessian output stream, initialized with an
  /// underlying output stream.
  ///
  /// <param name="os">the underlying output stream.</param>
  /// </summary>
  public HessianSerializerOutput(OutputStream os)
  {
    Super(os);
  }

  /// <summary>
  /// Creates an uninitialized Hessian output stream.
  /// </summary>
  public HessianSerializerOutput()
  {
    Super(null);
  }

  /// <summary>
  /// Applications which override this can do custom serialization.
  ///
  /// <param name="object">the object to write.</param>
  /// </summary>
  public void WriteObjectImpl(object obj)
      {
    Class cl = obj.GetClass();
    
    try {
      Method method = cl.GetMethod("writeReplace", new Class[0]);
      object repl = method.Invoke(obj, new Object[0]);

      WriteObject(repl);
      return;
    } catch (Exception e) {
    }

    try {
      WriteMapBegin(cl.GetName());
      for (; cl != null; cl = cl.GetSuperclass()) {
        Field[] fields = cl.GetDeclaredFields();
        for (int i = 0; i < fields.length; i++) {
          Field field = fields[i];

          if (Modifier.IsTransient(field.GetModifiers()) ||
              Modifier.IsStatic(field.GetModifiers()))
            continue;

          // XXX: could parameterize the handler to only deal with public
          field.SetAccessible(true);
      
          WriteString(field.GetName());
          WriteObject(field.Get(obj));
        }
      }
      WriteMapEnd();
    } catch (IllegalAccessException e) {
      throw new IOExceptionWrapper(e);
    }
  }
}

}