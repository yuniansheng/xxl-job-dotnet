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
public class JavaSerializer : AbstractSerializer
{
  private static readonly Logger log
    = Logger.GetLogger(JavaSerializer.class.GetName());

  private static readonly WeakHashMap<Type,SoftReference<JavaSerializer>> _serializerMap
    = new WeakHashMap<Type,SoftReference<JavaSerializer>>();

  private Field[] _fields;
  private FieldSerializer[] _fieldSerializers;

  private object _writeReplaceFactory;
  private Method _writeReplace;
  
  public JavaSerializer(Type cl)
  {
    Introspect(cl);

    _writeReplace = GetWriteReplace(cl);

    if (_writeReplace != null)
      _writeReplace.SetAccessible(true);
  }

  public static ISerializer Create(Type cl)
  {
    synchronized (_serializerMap) {
      SoftReference<JavaSerializer> baseRef
        = _serializerMap.Get(cl);
      
      JavaSerializer base = baseRef != null ? baseRef.Get() : null;

      if (base == null) {
        if (cl.IsAnnotationPresent(HessianUnshared.class))
          base = new JavaUnsharedSerializer(cl);
        else
          base = new JavaSerializer(cl);
        
        baseRef = new SoftReference<JavaSerializer>(base);
        _serializerMap.Put(cl, baseRef);
      }

      return base;
    }
  }

  protected void Introspect(Type cl)
  {
    if (_writeReplace != null)
      _writeReplace.SetAccessible(true);

    ArrayList<Field> primitiveFields = new ArrayList<Field>();
    ArrayList<Field> compoundFields = new ArrayList<Field>();
    
    for (; cl != null; cl = cl.GetSuperclass()) {
      Field[] fields = cl.GetDeclaredFields();
      for (int i = 0; i < fields.length; i++) {
        Field field = fields[i];

        if (Modifier.IsTransient(field.GetModifiers())
            || Modifier.IsStatic(field.GetModifiers()))
          continue;

        // XXX: could parameterize the handler to only deal with public
        field.SetAccessible(true);

        if (field.GetType().IsPrimitive()
            || (field.GetType().GetName().StartsWith("java.lang.")
                && ! field.GetType().Equals(Object.class)))
          primitiveFields.Add(field);
        else
          compoundFields.Add(field);
      }
    }

    ArrayList<Field> fields = new ArrayList<Field>();
    fields.AddAll(primitiveFields);
    fields.AddAll(compoundFields);

    _fields = new Field[fields.Size()];
    fields.ToArray(_fields);

    _fieldSerializers = new FieldSerializer[_fields.length];

    for (int i = 0; i < _fields.length; i++) {
      _fieldSerializers[i] = GetFieldSerializer(_fields[i].GetType());
    }
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

        if (method.GetName().Equals("writeReplace")
            && method.GetParameterTypes().length == 0)
          return method;
      }
    }

    return null;
  }

  /// <summary>
  /// Returns the writeReplace method
  /// </summary>
  protected Method GetWriteReplace(Type cl, Type param)
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
  
    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (out.AddRef(obj)) {
      return;
    }
    
    Type cl = obj.GetClass();

    try {
      if (_writeReplace != null) {
        object repl;

        if (_writeReplaceFactory != null)
          repl = _writeReplace.Invoke(_writeReplaceFactory, obj);
        else
          repl = _writeReplace.Invoke(obj);

        // out.RemoveRef(obj);

        /*
        out.WriteObject(repl);

        out.ReplaceRef(repl, obj);
       /// </summary>

        //hessian/3a5a
        int ref = out.WriteObjectBegin(cl.GetName());

        if (ref < -1) {
          WriteObject10(repl, out);
        } else {
          if (ref == -1) {
            WriteDefinition20(out);
            out.WriteObjectBegin(cl.GetName());
          }

          WriteInstance(repl, out);
        }

        return;
      }
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      // log.Log(Level.FINE, e.ToString(), e);
      throw new RuntimeException(e);
    }

    int ref = out.WriteObjectBegin(cl.GetName());

    if (ref < -1) {
      WriteObject10(obj, out);
    }
    else {
      if (ref == -1) {
        WriteDefinition20(out);
        out.WriteObjectBegin(cl.GetName());
      }

      WriteInstance(obj, out);
    }
  }
  
  protected void WriteObject10(object obj, AbstractHessianOutput out)
      {
    for (int i = 0; i < _fields.length; i++) {
      Field field = _fields[i];

      out.WriteString(field.GetName());

      _fieldSerializers[i].Serialize(out, obj, field);
    }
      
    out.WriteMapEnd();
  }
  
  private void WriteDefinition20(AbstractHessianOutput out)
      {
    out.WriteClassFieldLength(_fields.length);

    for (int i = 0; i < _fields.length; i++) {
      Field field = _fields[i];
      
      out.WriteString(field.GetName());
    }
  }
  
    public override void WriteInstance(object obj, AbstractHessianOutput out)
      {
    try {
      for (int i = 0; i < _fields.length; i++) {
        Field field = _fields[i];

        _fieldSerializers[i].Serialize(out, obj, field);
      }
    } catch (RuntimeException e) {
      throw new RuntimeException(e.GetMessage() + "\n class: "
                                 + obj.GetType().Name
                                 + " (object=" + obj + ")",
                                 e);
    } catch (IOException e) {
      throw new IOExceptionWrapper(e.GetMessage() + "\n class: "
                                   + obj.GetType().Name
                                   + " (object=" + obj + ")",
                                   e);
    }
  }

  private static FieldSerializer GetFieldSerializer(Type type)
  {
    if (int.class.Equals(type)
        || byte.class.Equals(type)
        || short.class.Equals(type)
        || int.class.Equals(type)) {
      return IntFieldSerializer.SER;
    }
    else if (long.class.Equals(type)) {
      return LongFieldSerializer.SER;
    }
    else if (double.class.Equals(type) ||
        float.class.Equals(type)) {
      return DoubleFieldSerializer.SER;
    }
    else if (bool.class.Equals(type)) {
      return BooleanFieldSerializer.SER;
    }
    else if (String.class.Equals(type)) {
      return StringFieldSerializer.SER;
    }
    else if (java.util.Date.class.Equals(type)
             || java.sql.Date.class.Equals(type)
             || java.sql.Timestamp.class.Equals(type)
             || java.sql.Time.class.Equals(type)) {
      return DateFieldSerializer.SER;
    }
    else
      return FieldSerializer.SER;
  }

  static class FieldSerializer {
    static readonly FieldSerializer SER = new FieldSerializer();
    
    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      object value = null;

      try {
        value = field.Get(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      try {
        out.WriteObject(value);
      } catch (RuntimeException e) {
        throw new RuntimeException(e.GetMessage() + "\n field: "
                                   + field.GetDeclaringClass().GetName()
                                   + '.' + field.GetName(),
                                   e);
      } catch (IOException e) {
        throw new IOExceptionWrapper(e.GetMessage() + "\n field: "
                                     + field.GetDeclaringClass().GetName()
                                     + '.' + field.GetName(),
                                     e);
      }
    }
  }

  static class BooleanFieldSerializer : FieldSerializer {
    static readonly FieldSerializer SER = new BooleanFieldSerializer();
    
    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      bool value = false;

      try {
        value = field.GetBoolean(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      out.WriteBoolean(value);
    }
  }

  static class IntFieldSerializer : FieldSerializer {
    static readonly FieldSerializer SER = new IntFieldSerializer();
    
    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      int value = 0;

      try {
        value = field.GetInt(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      out.WriteInt(value);
    }
  }

  static class LongFieldSerializer : FieldSerializer {
    static readonly FieldSerializer SER = new LongFieldSerializer();
    
    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      long value = 0;

      try {
        value = field.GetLong(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      out.WriteLong(value);
    }
  }

  static class DoubleFieldSerializer : FieldSerializer {
    static readonly FieldSerializer SER = new DoubleFieldSerializer();
    
    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      double value = 0;

      try {
        value = field.GetDouble(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      out.WriteDouble(value);
    }
  }

  static class StringFieldSerializer : FieldSerializer {
    static readonly FieldSerializer SER = new StringFieldSerializer();
    
    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      string value = null;

      try {
        value = (String) field.Get(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      out.WriteString(value);
    }
  }

  static class DateFieldSerializer : FieldSerializer {
    static readonly FieldSerializer SER = new DateFieldSerializer();

    void Serialize(AbstractHessianOutput out, object obj, Field field)
          {
      java.util.Date value = null;

      try {
        value = (java.util.Date) field.Get(obj);
      } catch (IllegalAccessException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }

      if (value == null)
        out.WriteNull();
      else
        out.WriteUTCDate(value.GetTime());
    }
  }
}

}