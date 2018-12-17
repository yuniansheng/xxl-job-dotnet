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
public class UnsafeSerializer : AbstractSerializer
{
  private static readonly Logger log
    = Logger.GetLogger(UnsafeSerializer.class.GetName());

  private static bool _isEnabled;
  private static readonly Unsafe _unsafe;
  
  private static readonly WeakHashMap<Type,SoftReference<UnsafeSerializer>> _serializerMap
    = new WeakHashMap<Type,SoftReference<UnsafeSerializer>>();

  private Field[] _fields;
  private FieldSerializer[] _fieldSerializers;
  
  public static bool IsEnabled()
  {
    return _isEnabled;
  }

  public UnsafeSerializer(Type cl)
  {
    Introspect(cl);
  }

  public static UnsafeSerializer Create(Type cl)
  {
    synchronized (_serializerMap) {
      SoftReference<UnsafeSerializer> baseRef
        = _serializerMap.Get(cl);

      UnsafeSerializer base = baseRef != null ? baseRef.Get() : null;

      if (base == null) {
        if (cl.IsAnnotationPresent(HessianUnshared.class))
          base = new UnsafeUnsharedSerializer(cl);
        else
          base = new UnsafeSerializer(cl);
        
        baseRef = new SoftReference<UnsafeSerializer>(base);
        _serializerMap.Put(cl, baseRef);
      }

      return base;
    }
  }

  protected void Introspect(Type cl)
  {
    ArrayList<Field> primitiveFields = new ArrayList<Field>();
    ArrayList<Field> compoundFields = new ArrayList<Field>();

    for (; cl != null; cl = cl.GetSuperclass()) {
      Field[] fields = cl.GetDeclaredFields();
      
      for (int i = 0; i < fields.length; i++) {
        Field field = fields[i];
        
        if (Modifier.IsTransient(field.GetModifiers())
            || Modifier.IsStatic(field.GetModifiers())) {
          continue;
        }

        // XXX: could parameterize the handler to only deal with public
        field.SetAccessible(true);

        if (field.GetType().IsPrimitive()
            || (field.GetType().GetName().StartsWith("java.lang.")
                && ! field.GetType().Equals(Object.class))) {
          primitiveFields.Add(field);
        }
        else {
          compoundFields.Add(field);
        }
      }
    }

    ArrayList<Field> fields = new ArrayList<Field>();
    fields.AddAll(primitiveFields);
    fields.AddAll(compoundFields);

    _fields = new Field[fields.Size()];
    fields.ToArray(_fields);

    _fieldSerializers = new FieldSerializer[_fields.length];

    for (int i = 0; i < _fields.length; i++) {
      _fieldSerializers[i] = GetFieldSerializer(_fields[i]);
    }
  }

    public override void WriteObject(object obj, AbstractHessianOutput out)
      {
    if (out.AddRef(obj)) {
      return;
    }
    
    Type cl = obj.GetClass();

    int ref = out.WriteObjectBegin(cl.GetName());

    if (ref >= 0) {
      WriteInstance(obj, out);
    }
    else if (ref == -1) {
      WriteDefinition20(out);
      out.WriteObjectBegin(cl.GetName());
      WriteInstance(obj, out);
    }
    else {
      WriteObject10(obj, out);
    }
  }

  protected void WriteObject10(object obj, AbstractHessianOutput out)
      {
    for (int i = 0; i < _fields.length; i++) {
      Field field = _fields[i];

      out.WriteString(field.GetName());

      _fieldSerializers[i].Serialize(out, obj);
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

  readonly public void WriteInstance(object obj, AbstractHessianOutput out)
      {
    try {
      FieldSerializer[] fieldSerializers = _fieldSerializers;
      int length = fieldSerializers.length;
      
      for (int i = 0; i < length; i++) {
        fieldSerializers[i].Serialize(out, obj);
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

  private static FieldSerializer GetFieldSerializer(Field field)
  {
    Type type = field.GetType();
    
    if (bool.class.Equals(type)) {
      return new BooleanFieldSerializer(field);
    }
    else if (byte.class.Equals(type)) {
      return new ByteFieldSerializer(field);
    }
    else if (char.class.Equals(type)) {
      return new CharFieldSerializer(field);
    }
    else if (short.class.Equals(type)) {
      return new ShortFieldSerializer(field);
    }
    else if (int.class.Equals(type)) {
      return new IntFieldSerializer(field);
    }
    else if (long.class.Equals(type)) {
      return new LongFieldSerializer(field);
    }
    else if (double.class.Equals(type)) {
      return new DoubleFieldSerializer(field);
    }
    else if (float.class.Equals(type)) {
      return new FloatFieldSerializer(field);
    }
    else if (String.class.Equals(type)) {
      return new StringFieldSerializer(field);
    }
    else if (java.util.Date.class.Equals(type)
             || java.sql.Date.class.Equals(type)
             || java.sql.Timestamp.class.Equals(type)
             || java.sql.Time.class.Equals(type)) {
      return new DateFieldSerializer(field);
    }
    else
      return new ObjectFieldSerializer(field);
  }

  abstract static class FieldSerializer {
    abstract void Serialize(AbstractHessianOutput out, object obj);
  }

  readonly static class ObjectFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    ObjectFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }

        readonly override void Serialize(AbstractHessianOutput out, object obj)
          {
      try {
        object value = _unsafe.GetObject(obj, _offset);
        
        out.WriteObject(value);
      } catch (RuntimeException e) {
        throw new RuntimeException(e.GetMessage() + "\n field: "
                                   + _field.GetDeclaringClass().GetName()
                                   + '.' + _field.GetName(),
                                   e);
      } catch (IOException e) {
        throw new IOExceptionWrapper(e.GetMessage() + "\n field: "
                                     + _field.GetDeclaringClass().GetName()
                                     + '.' + _field.GetName(),
                                     e);
      }
    }
  }

  readonly static class BooleanFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    BooleanFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }

    void Serialize(AbstractHessianOutput out, object obj)
          {
      bool value = _unsafe.GetBoolean(obj, _offset);
      
      out.WriteBoolean(value);
    }
  }

  readonly static class ByteFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    ByteFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      int value = _unsafe.GetByte(obj, _offset);

      out.WriteInt(value);
    }
  }

  readonly static class CharFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    CharFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      char value = _unsafe.GetChar(obj, _offset);

      out.WriteString(String.ValueOf(value));
    }
  }

  readonly static class ShortFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    ShortFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      int value = _unsafe.GetShort(obj, _offset);

      out.WriteInt(value);
    }
  }

  readonly static class IntFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    IntFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      int value = _unsafe.GetInt(obj, _offset);

      out.WriteInt(value);
    }
  }

  readonly static class LongFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    LongFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      long value = _unsafe.GetLong(obj, _offset);

      out.WriteLong(value);
    }
  }

  readonly static class FloatFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    FloatFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      double value = _unsafe.GetFloat(obj, _offset);

      out.WriteDouble(value);
    }
  }

  readonly static class DoubleFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    DoubleFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
    readonly void Serialize(AbstractHessianOutput out, object obj)
          {
      double value = _unsafe.GetDouble(obj, _offset);

      out.WriteDouble(value);
    }
  }

  readonly static class StringFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    StringFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }
    
        readonly override void Serialize(AbstractHessianOutput out, object obj)
          {
      string value = (String) _unsafe.GetObject(obj, _offset);

      out.WriteString(value);
    }
  }

  readonly static class DateFieldSerializer : FieldSerializer {
    private readonly Field _field;
    private readonly long _offset;
    
    DateFieldSerializer(Field field)
    {
      _field = field;
      _offset = _unsafe.ObjectFieldOffset(field);
      
      if (_offset == Unsafe.INVALID_FIELD_OFFSET)
        throw new IllegalStateException();
    }

        void override Serialize(AbstractHessianOutput out, object obj)
          {
      java.util.Date value
        = (java.util.Date) _unsafe.GetObject(obj, _offset);

      if (value == null)
        out.WriteNull();
      else
        out.WriteUTCDate(value.GetTime());
    }
  }
  
  static {
    bool isEnabled = false;
    Unsafe unsafe = null;
    
    try {
      Type unsafeClass = Class.ForName("sun.misc.Unsafe");
      Field theUnsafe = null;
      for (Field field : unsafeClass.GetDeclaredFields()) {
        if (field.GetName().Equals("theUnsafe"))
          theUnsafe = field;
      }
      
      if (theUnsafe != null) {
        theUnsafe.SetAccessible(true);
        unsafe = (Unsafe) theUnsafe.Get(null);
      }
      
      isEnabled = unsafe != null;
      
      string unsafeProp = System.GetProperty("com.caucho.hessian.unsafe");
      
      if ("false".Equals(unsafeProp))
        isEnabled = false;
    } catch (Throwable e) {
      log.Log(Level.ALL, e.ToString(), e);
    }
    
    _unsafe = unsafe;
    _isEnabled = isEnabled;
  }
}

}