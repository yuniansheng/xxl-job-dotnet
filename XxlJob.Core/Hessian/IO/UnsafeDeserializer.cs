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
public class UnsafeDeserializer : AbstractMapDeserializer {
  private static readonly Logger log
    = Logger.GetLogger(JavaDeserializer.class.GetName());

  private static bool _isEnabled;
  @SuppressWarnings("restriction")
  private static Unsafe _unsafe;

  private Type _type;
  private HashMap<String,FieldDeserializer> _fieldMap;
  private Method _readResolve;

  public UnsafeDeserializer(Type cl)
  {
    _type = cl;
    _fieldMap = GetFieldMap(cl);

    _readResolve = GetReadResolve(cl);

    if (_readResolve != null) {
      _readResolve.SetAccessible(true);
    }
  }
  
  public static bool IsEnabled()
  {
    return _isEnabled;
  }

    public override Type GetType()
  {
    return _type;
  }

    public override bool IsReadResolve()
  {
    return _readResolve != null;
  }

  public object ReadMap(AbstractHessianInput in)
      {
    try {
      object obj = Instantiate();

      return ReadMap(in, obj);
    } catch (IOException e) {
      throw e;
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(_type.GetName() + ":" + e.GetMessage(), e);
    }
  }
  
    public override object[] CreateFields(int len)
  {
    return new FieldDeserializer[len];
  }

  public object CreateField(string name)
  {
    object reader = _fieldMap.Get(name);
    
    if (reader == null)
      reader = NullFieldDeserializer.DESER;
    
    return reader;
  }

    public override object ReadObject(AbstractHessianInput in,
                           object[] fields)
      {
    try {
      object obj = Instantiate();

      return ReadObject(in, obj, (FieldDeserializer[] ) fields);
    } catch (IOException e) {
      throw e;
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(_type.GetName() + ":" + e.GetMessage(), e);
    }
  }

    public override object ReadObject(AbstractHessianInput in,
                           string[] fieldNames)
      {
    try {
      object obj = Instantiate();

      return ReadObject(in, obj, fieldNames);
    } catch (IOException e) {
      throw e;
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(_type.GetName() + ":" + e.GetMessage(), e);
    }
  }

  /// <summary>
  /// Returns the readResolve method
  /// </summary>
  protected Method GetReadResolve(Type cl)
  {
    for (; cl != null; cl = cl.GetSuperclass()) {
      Method[] methods = cl.GetDeclaredMethods();

      for (int i = 0; i < methods.length; i++) {
        Method method = methods[i];

        if (method.GetName().Equals("readResolve")
            && method.GetParameterTypes().length == 0)
          return method;
      }
    }

    return null;
  }

  public object ReadMap(AbstractHessianInput in, object obj)
      {
    try {
      int ref = in.AddRef(obj);

      while (! in.IsEnd()) {
        object key = in.ReadObject();

        FieldDeserializer deser = (FieldDeserializer) _fieldMap.Get(key);

        if (deser != null)
          deser.Deserialize(in, obj);
        else
          in.ReadObject();
      }

      in.ReadMapEnd();

      object resolve = Resolve(in, obj);

      if (obj != resolve)
        in.SetRef(ref, resolve);

      return resolve;
    } catch (IOException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(e);
    }
  }

  public object ReadObject(AbstractHessianInput in,
                           object obj,
                           FieldDeserializer[] fields)
      {
    try {
      int ref = in.AddRef(obj);

      for (FieldDeserializer reader : fields) {
        reader.Deserialize(in, obj);
      }

      object resolve = Resolve(in, obj);

      if (obj != resolve)
        in.SetRef(ref, resolve);

      return resolve;
    } catch (IOException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(obj.GetType().Name + ":" + e, e);
    }
  }

  public object ReadObject(AbstractHessianInput in,
                           object obj,
                           string[] fieldNames)
      {
    try {
      int ref = in.AddRef(obj);

      for (string fieldName : fieldNames) {
        FieldDeserializer reader = _fieldMap.Get(fieldName);
        
        if (reader != null)
          reader.Deserialize(in, obj);
        else
          in.ReadObject();
      }

      object resolve = Resolve(in, obj);

      if (obj != resolve)
        in.SetRef(ref, resolve);

      return resolve;
    } catch (IOException e) {
      throw e;
    } catch (Exception e) {
      throw new IOExceptionWrapper(obj.GetType().Name + ":" + e, e);
    }
  }

  protected object Resolve(AbstractHessianInput in, object obj)
  {
    // if there's a readResolve method, call it
    try {
      if (_readResolve != null)
        return _readResolve.Invoke(obj, new Object[0]);
    } catch (InvocationTargetException e) {
      if (e.GetCause() instanceof Exception)
        throw (Exception) e.GetCause();
      else
        throw e;
    }

    return obj;
  }

  @SuppressWarnings("restriction")
  protected object Instantiate()
  {
    return _unsafe.AllocateInstance(_type);
  }

  /// <summary>
  /// Creates a map of the classes fields.
  /// </summary>
  protected HashMap<String,FieldDeserializer> GetFieldMap(Type cl)
  {
    HashMap<String,FieldDeserializer> fieldMap
      = new HashMap<String,FieldDeserializer>();

    for (; cl != null; cl = cl.GetSuperclass()) {
      Field[] fields = cl.GetDeclaredFields();
      for (int i = 0; i < fields.length; i++) {
        Field field = fields[i];

        if (Modifier.IsTransient(field.GetModifiers())
            || Modifier.IsStatic(field.GetModifiers()))
          continue;
        else if (fieldMap.Get(field.GetName()) != null)
          continue;

        // XXX: could parameterize the handler to only deal with public
        try {
          field.SetAccessible(true);
        } catch (Throwable e) {
          e.PrintStackTrace();
        }

        Type type = field.GetType();
        FieldDeserializer deser;

        if (String.class.Equals(type)) {
          deser = new StringFieldDeserializer(field);
        }
        else if (byte.class.Equals(type)) {
          deser = new ByteFieldDeserializer(field);
        }
        else if (char.class.Equals(type)) {
          deser = new CharFieldDeserializer(field);
        }
        else if (short.class.Equals(type)) {
          deser = new ShortFieldDeserializer(field);
        }
        else if (int.class.Equals(type)) {
          deser = new IntFieldDeserializer(field);
        }
        else if (long.class.Equals(type)) {
          deser = new LongFieldDeserializer(field);
        }
        else if (float.class.Equals(type)) {
          deser = new FloatFieldDeserializer(field);
        }
        else if (double.class.Equals(type)) {
          deser = new DoubleFieldDeserializer(field);
        }
        else if (bool.class.Equals(type)) {
          deser = new BooleanFieldDeserializer(field);
        }
        else if (java.sql.Date.class.Equals(type)) {
          deser = new SqlDateFieldDeserializer(field);
  }
        else if (java.sql.Timestamp.class.Equals(type)) {
          deser = new SqlTimestampFieldDeserializer(field);
  }
        else if (java.sql.Time.class.Equals(type)) {
          deser = new SqlTimeFieldDeserializer(field);
  }
        else {
          deser = new ObjectFieldDeserializer(field);
        }

        fieldMap.Put(field.GetName(), deser);
      }
    }

    return fieldMap;
  }

  abstract static class FieldDeserializer {
    abstract void Deserialize(AbstractHessianInput in, object obj);
  }
  
  static class NullFieldDeserializer : FieldDeserializer {
    static NullFieldDeserializer DESER = new NullFieldDeserializer();
    
        void override Deserialize(AbstractHessianInput in, object obj)
          {
      in.ReadObject();
    }
  }

  static class ObjectFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    ObjectFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      object value = null;
      
      try {
        value = in.ReadObject(_field.GetType());

        _unsafe.PutObject(obj, _offset, value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class BooleanFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    BooleanFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      bool value = false;
      
      try {
        value = in.ReadBoolean();

        _unsafe.PutBoolean(obj, _offset, value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class ByteFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    ByteFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      int value = 0;
      
      try {
        value = in.ReadInt();

        _unsafe.PutByte(obj, _offset, (byte) value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class CharFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    CharFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      string value = null;
      
      try {
        value = in.ReadString();
        
        char ch;
        
        if (value != null && value.Length() > 0)
          ch = value.CharAt(0);
        else
          ch = 0;

        _unsafe.PutChar(obj, _offset, ch);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class ShortFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    ShortFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      int value = 0;
      
      try {
        value = in.ReadInt();

        _unsafe.PutShort(obj, _offset, (short) value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class IntFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    IntFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      int value = 0;
      
      try {
        value = in.ReadInt();

        _unsafe.PutInt(obj, _offset, value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class LongFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    LongFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      long value = 0;
      
      try {
        value = in.ReadLong();

        _unsafe.PutLong(obj, _offset, value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class FloatFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    FloatFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    void Deserialize(AbstractHessianInput in, object obj)
          {
      double value = 0;
      
      try {
        value = in.ReadDouble();

        _unsafe.PutFloat(obj, _offset, (float) value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class DoubleFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    DoubleFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      double value = 0;
      
      try {
        value = in.ReadDouble();

        _unsafe.PutDouble(obj, _offset, value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class StringFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    StringFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      string value = null;
      
      try {
        value = in.ReadString();

        _unsafe.PutObject(obj, _offset, value);
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class SqlDateFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    SqlDateFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      java.sql.Date value = null;

      try {
        java.util.Date date = (java.util.Date) in.ReadObject();
        
        if (date != null) {
          value = new java.sql.Date(date.GetTime());

          _unsafe.PutObject(obj, _offset, value);
        } else {
          _unsafe.PutObject(obj, _offset, null);
        }
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class SqlTimestampFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    SqlTimestampFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      java.sql.Timestamp value = null;

      try {
        java.util.Date date = (java.util.Date) in.ReadObject();
        
        if (date != null) {
          value = new java.sql.Timestamp(date.GetTime());

          _unsafe.PutObject(obj, _offset, value);
        }
        else {
          _unsafe.PutObject(obj, _offset, null);
        }
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static class SqlTimeFieldDeserializer : FieldDeserializer {
    private readonly Field _field;
    private readonly long _offset;

    @SuppressWarnings("restriction")
    SqlTimeFieldDeserializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(_field);
    }

    @SuppressWarnings("restriction")
    void Deserialize(AbstractHessianInput in, object obj)
          {
      java.sql.Time value = null;

      try {
        java.util.Date date = (java.util.Date) in.ReadObject();
        
        if (date != null) {
          value = new java.sql.Time(date.GetTime());

          _unsafe.PutObject(obj, _offset, value);
        } else {
          _unsafe.PutObject(obj, _offset, null);
        }
      } catch (Exception e) {
        LogDeserializeError(_field, obj, value, e);
      }
    }
  }

  static void LogDeserializeError(Field field, object obj, object value,
                                  Throwable e)
      {
    string fieldName = (field.GetDeclaringClass().GetName()
                        + "." + field.GetName());

    if (e instanceof HessianFieldException)
      throw (HessianFieldException) e;
    else if (e instanceof IOException)
      throw new HessianFieldException(fieldName + ": " + e.GetMessage(), e);

    if (value != null)
      throw new HessianFieldException(fieldName + ": " + value.GetType().Name + " (" + value + ")"
                                      + " cannot be assigned to '" + field.GetType().GetName() + "'", e);
    else
       throw new HessianFieldException(fieldName + ": " + field.GetType().GetName() + " cannot be assigned from null", e);
  }

  static {
    bool isEnabled = false;

    try {
      Type unsafe = Class.ForName("sun.misc.Unsafe");
      Field theUnsafe = null;
      for (Field field : unsafe.GetDeclaredFields()) {
        if (field.GetName().Equals("theUnsafe"))
          theUnsafe = field;
      }

      if (theUnsafe != null) {
        theUnsafe.SetAccessible(true);
        _unsafe = (Unsafe) theUnsafe.Get(null);
      }

      isEnabled = _unsafe != null;

      string unsafeProp = System.GetProperty("com.caucho.hessian.unsafe");

      if ("false".Equals(unsafeProp))
        isEnabled = false;
    } catch (Throwable e) {
      log.Log(Level.FINER, e.ToString(), e);
    }

    _isEnabled = isEnabled;
  }
}

}