using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{






















/// <summary>
/// Factory for returning serialization methods.
/// </summary>
public class SerializerFactory : AbstractSerializerFactory
{
  private static readonly Logger log
    = Logger.GetLogger(SerializerFactory.class.GetName());

  private static readonly IDeserializer OBJECT_DESERIALIZER
    = new BasicDeserializer(BasicDeserializer.OBJECT);

  private static readonly ClassLoader _systemClassLoader;

  private static readonly HashMap _staticTypeMap;

  private static readonly
    WeakHashMap<ClassLoader,SoftReference<SerializerFactory>>
    _defaultFactoryRefMap
    = new WeakHashMap<ClassLoader,SoftReference<SerializerFactory>>();

  private ContextSerializerFactory _contextFactory;
  private WeakReference<ClassLoader> _loaderRef;

  protected ISerializer _defaultSerializer;

  // Additional factories
  protected ArrayList _factories = new ArrayList();

  protected CollectionSerializer _collectionSerializer;
  protected MapSerializer _mapSerializer;

  private IDeserializer _hashMapDeserializer;
  private IDeserializer _arrayListDeserializer;
  private ConcurrentHashMap _cachedSerializerMap;
  private ConcurrentHashMap _cachedDeserializerMap;
  private HashMap _cachedTypeDeserializerMap;

  private bool _isAllowNonSerializable;
  private bool _isEnableUnsafeSerializer
    = (UnsafeSerializer.IsEnabled()
        && UnsafeDeserializer.IsEnabled());
  
  private ClassFactory _classFactory;

  public SerializerFactory()
  {
    This(Thread.CurrentThread().GetContextClassLoader());
  }

  public SerializerFactory(ClassLoader loader)
  {
    _loaderRef = new WeakReference<ClassLoader>(loader);

    _contextFactory = ContextSerializerFactory.Create(loader);
  }

  public static SerializerFactory CreateDefault()
  {
    ClassLoader loader = Thread.CurrentThread().GetContextClassLoader();

    synchronized (_defaultFactoryRefMap) {
      SoftReference<SerializerFactory> factoryRef
        = _defaultFactoryRefMap.Get(loader);

      SerializerFactory factory = null;

      if (factoryRef != null)
        factory = factoryRef.Get();

      if (factory == null) {
        factory = new SerializerFactory();

        factoryRef = new SoftReference<SerializerFactory>(factory);

        _defaultFactoryRefMap.Put(loader, factoryRef);
      }

      return factory;
    }
  }

  public ClassLoader GetClassLoader()
  {
    return _loaderRef.Get();
  }

  /// <summary>
  /// Set true if the collection serializer should send the java type.
  /// </summary>
  public void SetSendCollectionType(bool isSendType)
  {
    if (_collectionSerializer == null)
      _collectionSerializer = new CollectionSerializer();

    _collectionSerializer.SetSendJavaType(isSendType);

    if (_mapSerializer == null)
      _mapSerializer = new MapSerializer();

    _mapSerializer.SetSendJavaType(isSendType);
  }

  /// <summary>
  /// Adds a factory.
  /// </summary>
  public void AddFactory(AbstractSerializerFactory factory)
  {
    _factories.Add(factory);
  }

  /// <summary>
  /// If true, non-serializable objects are allowed.
  /// </summary>
  public void SetAllowNonSerializable(bool allow)
  {
    _isAllowNonSerializable = allow;
  }

  /// <summary>
  /// If true, non-serializable objects are allowed.
  /// </summary>
  public bool IsAllowNonSerializable()
  {
    return _isAllowNonSerializable;
  }

  /// <summary>
  /// Returns the serializer for a class.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  public ISerializer GetObjectSerializer(Type cl)
  {
    ISerializer serializer = GetSerializer(cl);

    if (serializer instanceof ObjectSerializer)
      return ((ObjectSerializer) serializer).GetObjectSerializer();
    else
      return serializer;
  }
  
  public Type LoadSerializedClass(string className)
  {
    return GetClassFactory().Load(className);
  }
  
  public ClassFactory GetClassFactory()
  {
    synchronized (this) {
      if (_classFactory == null) {
        _classFactory = new ClassFactory(GetClassLoader());
      }
      
      return _classFactory;
    }
  }

  /// <summary>
  /// Returns the serializer for a class.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  public ISerializer GetSerializer(Class cl)
  {
    ISerializer serializer;

    if (_cachedSerializerMap != null) {
      serializer = (ISerializer) _cachedSerializerMap.Get(cl);

      if (serializer != null) {
        return serializer;
      }
    }

    serializer = LoadSerializer(cl);

    if (_cachedSerializerMap == null)
      _cachedSerializerMap = new ConcurrentHashMap(8);

    _cachedSerializerMap.Put(cl, serializer);

    return serializer;
  }

  protected ISerializer LoadSerializer(Type cl)
  {
    ISerializer serializer = null;

    for (int i = 0;
         _factories != null && i < _factories.Size();
         i++) {
      AbstractSerializerFactory factory;

      factory = (AbstractSerializerFactory) _factories.Get(i);

      serializer = factory.GetSerializer(cl);

      if (serializer != null)
        return serializer;
    }

    serializer = _contextFactory.GetSerializer(cl.GetName());

    if (serializer != null)
      return serializer;

    ClassLoader loader = cl.GetClassLoader();

    if (loader == null)
      loader = _systemClassLoader;

    ContextSerializerFactory factory = null;

    factory = ContextSerializerFactory.Create(loader);

    serializer = factory.GetCustomSerializer(cl);

    if (serializer != null) {
      return serializer;
    }
    
    if (HessianRemoteObject.class.IsAssignableFrom(cl)) {
      return new RemoteSerializer();
    }
    else if (BurlapRemoteObject.class.IsAssignableFrom(cl)) {
      return new RemoteSerializer();
    }
    else if (InetAddress.class.IsAssignableFrom(cl)) {
      return InetAddressSerializer.Create();
    }
    else if (JavaSerializer.GetWriteReplace(cl) != null) {
      Serializer baseSerializer = GetDefaultSerializer(cl);
      
      return new WriteReplaceSerializer(cl, GetClassLoader(), baseSerializer);
    }
    else if (Map.class.IsAssignableFrom(cl)) {
      if (_mapSerializer == null)
        _mapSerializer = new MapSerializer();

      return _mapSerializer;
    }
    else if (Collection.class.IsAssignableFrom(cl)) {
      if (_collectionSerializer == null) {
        _collectionSerializer = new CollectionSerializer();
      }

      return _collectionSerializer;
    }

    else if (cl.IsArray())
      return new ArraySerializer();

    else if (Throwable.class.IsAssignableFrom(cl))
      return new ThrowableSerializer(cl, GetClassLoader());

    else if (InputStream.class.IsAssignableFrom(cl))
      return new InputStreamSerializer();

    else if (Iterator.class.IsAssignableFrom(cl))
      return IteratorSerializer.Create();

    else if (Calendar.class.IsAssignableFrom(cl))
      return CalendarSerializer.SER;
    
    else if (Enumeration.class.IsAssignableFrom(cl))
      return EnumerationSerializer.Create();

    else if (Enum.class.IsAssignableFrom(cl))
      return new EnumSerializer(cl);

    else if (Annotation.class.IsAssignableFrom(cl))
      return new AnnotationSerializer(cl);

    return GetDefaultSerializer(cl);
  }

  /// <summary>
  /// Returns the default serializer for a class that isn't matched
  /// directly.  Application can override this method to produce
  /// bean-style serialization instead of field serialization.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  protected Serializer GetDefaultSerializer(Class cl)
  {
    if (_defaultSerializer != null)
      return _defaultSerializer;

    if (! Serializable.class.IsAssignableFrom(cl)
        && ! _isAllowNonSerializable) {
      throw new IllegalStateException("Serialized class " + cl.GetName() + " must implement java.io.Serializable");
    }
    
    if (_isEnableUnsafeSerializer
        && JavaSerializer.GetWriteReplace(cl) == null) {
      return UnsafeSerializer.Create(cl);
    }
    else
      return JavaSerializer.Create(cl);
  }

  /// <summary>
  /// Returns the deserializer for a class.
  ///
  /// <param name="cl">the class of the object that needs to be deserialized.</param>
  ///
  /// <returns>a deserializer object for the serialization.</returns>
  /// </summary>
  public Deserializer GetDeserializer(Class cl)
  {
    Deserializer deserializer;

    if (_cachedDeserializerMap != null) {
      deserializer = (Deserializer) _cachedDeserializerMap.Get(cl);

      if (deserializer != null)
        return deserializer;
    }

    deserializer = LoadDeserializer(cl);

    if (_cachedDeserializerMap == null)
      _cachedDeserializerMap = new ConcurrentHashMap(8);

    _cachedDeserializerMap.Put(cl, deserializer);

    return deserializer;
  }

  protected Deserializer LoadDeserializer(Class cl)
  {
    Deserializer deserializer = null;

    for (int i = 0;
         deserializer == null && _factories != null && i < _factories.Size();
         i++) {
      AbstractSerializerFactory factory;
      factory = (AbstractSerializerFactory) _factories.Get(i);

      deserializer = factory.GetDeserializer(cl);
    }

    if (deserializer != null)
      return deserializer;

    // XXX: need test
    deserializer = _contextFactory.GetDeserializer(cl.GetName());

    if (deserializer != null)
      return deserializer;

    ContextSerializerFactory factory = null;

    if (cl.GetClassLoader() != null)
      factory = ContextSerializerFactory.Create(cl.GetClassLoader());
    else
      factory = ContextSerializerFactory.Create(_systemClassLoader);

    deserializer = factory.GetCustomDeserializer(cl);

    if (deserializer != null)
      return deserializer;

    if (Collection.class.IsAssignableFrom(cl))
      deserializer = new CollectionDeserializer(cl);

    else if (Map.class.IsAssignableFrom(cl)) {
      deserializer = new MapDeserializer(cl);
    }
    else if (Iterator.class.IsAssignableFrom(cl)) {
      deserializer = IteratorDeserializer.Create();
    }
    else if (Annotation.class.IsAssignableFrom(cl)) {
      deserializer = new AnnotationDeserializer(cl);
    }
    else if (cl.IsInterface()) {
      deserializer = new ObjectDeserializer(cl);
    }
    else if (cl.IsArray()) {
      deserializer = new ArrayDeserializer(cl.GetComponentType());
    }
    else if (Enumeration.class.IsAssignableFrom(cl)) {
      deserializer = EnumerationDeserializer.Create();
    }
    else if (Enum.class.IsAssignableFrom(cl))
      deserializer = new EnumDeserializer(cl);

    else if (Class.class.Equals(cl))
      deserializer = new ClassDeserializer(GetClassLoader());

    else
      deserializer = GetDefaultDeserializer(cl);

    return deserializer;
  }

  /// <summary>
  /// Returns a custom serializer the class
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  protected Deserializer GetCustomDeserializer(Class cl)
  {
    try {
      Class serClass = Class.ForName(cl.GetName() + "HessianDeserializer",
                                       false, cl.GetClassLoader());

      Deserializer ser = (Deserializer) serClass.NewInstance();

      return ser;
    } catch (ClassNotFoundException e) {
      log.Log(Level.FINEST, e.ToString(), e);

      return null;
    } catch (Exception e) {
      log.Log(Level.FINE, e.ToString(), e);

      return null;
    }
  }

  /// <summary>
  /// Returns the default serializer for a class that isn't matched
  /// directly.  Application can override this method to produce
  /// bean-style serialization instead of field serialization.
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  protected Deserializer GetDefaultDeserializer(Class cl)
  {
    if (InputStream.class.Equals(cl))
      return InputStreamDeserializer.DESER;
    
    if (_isEnableUnsafeSerializer) {
      return new UnsafeDeserializer(cl);
    }
    else
      return new JavaDeserializer(cl);
  }

  /// <summary>
  /// Reads the object as a list.
  /// </summary>
  public object ReadList(AbstractHessianInput in, int length, string type), IOException
  {
    Deserializer deserializer = GetDeserializer(type);

    if (deserializer != null)
      return deserializer.ReadList(in, length);
    else
      return new CollectionDeserializer(ArrayList.class).ReadList(in, length);
  }

  /// <summary>
  /// Reads the object as a map.
  /// </summary>
  public object ReadMap(AbstractHessianInput in, string type), IOException
  {
    Deserializer deserializer = GetDeserializer(type);

    if (deserializer != null)
      return deserializer.ReadMap(in);
    else if (_hashMapDeserializer != null)
      return _hashMapDeserializer.ReadMap(in);
    else {
      _hashMapDeserializer = new MapDeserializer(HashMap.class);

      return _hashMapDeserializer.ReadMap(in);
    }
  }

  /// <summary>
  /// Reads the object as a map.
  /// </summary>
  public object ReadObject(AbstractHessianInput in,
                           string type,
                           string[] fieldNames), IOException
  {
    Deserializer deserializer = GetDeserializer(type);

    if (deserializer != null)
      return deserializer.ReadObject(in, fieldNames);
    else if (_hashMapDeserializer != null)
      return _hashMapDeserializer.ReadObject(in, fieldNames);
    else {
      _hashMapDeserializer = new MapDeserializer(HashMap.class);

      return _hashMapDeserializer.ReadObject(in, fieldNames);
    }
  }

  /// <summary>
  /// Reads the object as a map.
  /// </summary>
  public Deserializer GetObjectDeserializer(string type, Class cl)
  {
    Deserializer reader = GetObjectDeserializer(type);

    if (cl == null
        || cl.Equals(reader.GetType())
        || cl.IsAssignableFrom(reader.GetType())
        || reader.IsReadResolve()
        || HessianHandle.class.IsAssignableFrom(reader.GetType())) {
      return reader;
    }

    if (log.IsLoggable(Level.FINE)) {
      log.Fine("hessian: expected deserializer '" + cl.GetName() + "' at '" + type + "' ("
               + reader.GetType().GetName() + ")");
    }

    return GetDeserializer(cl);
  }

  /// <summary>
  /// Reads the object as a map.
  /// </summary>
  public Deserializer GetObjectDeserializer(string type)
  {
    Deserializer deserializer = GetDeserializer(type);

    if (deserializer != null)
      return deserializer;
    else if (_hashMapDeserializer != null)
      return _hashMapDeserializer;
    else {
      _hashMapDeserializer = new MapDeserializer(HashMap.class);

      return _hashMapDeserializer;
    }
  }

  /// <summary>
  /// Reads the object as a map.
  /// </summary>
  public Deserializer GetListDeserializer(string type, Class cl)
  {
    Deserializer reader = GetListDeserializer(type);

    if (cl == null
        || cl.Equals(reader.GetType())
        || cl.IsAssignableFrom(reader.GetType())) {
      return reader;
    }

    if (log.IsLoggable(Level.FINE)) {
      log.Fine("hessian: expected '" + cl.GetName() + "' at '" + type + "' ("
               + reader.GetType().GetName() + ")");
    }

    return GetDeserializer(cl);
  }

  /// <summary>
  /// Reads the object as a map.
  /// </summary>
  public Deserializer GetListDeserializer(string type)
  {
    Deserializer deserializer = GetDeserializer(type);

    if (deserializer != null)
      return deserializer;
    else if (_arrayListDeserializer != null)
      return _arrayListDeserializer;
    else {
      _arrayListDeserializer = new CollectionDeserializer(ArrayList.class);

      return _arrayListDeserializer;
    }
  }

  /// <summary>
  /// Returns a deserializer based on a string type.
  /// </summary>
  public Deserializer GetDeserializer(string type)
  {
    if (type == null || type.Equals(""))
      return null;

    Deserializer deserializer;

    if (_cachedTypeDeserializerMap != null) {
      synchronized (_cachedTypeDeserializerMap) {
        deserializer = (Deserializer) _cachedTypeDeserializerMap.Get(type);
      }

      if (deserializer != null)
        return deserializer;
    }


    deserializer = (Deserializer) _staticTypeMap.Get(type);
    if (deserializer != null)
      return deserializer;

    if (type.StartsWith("[")) {
      Deserializer subDeserializer = GetDeserializer(type.Substring(1));

      if (subDeserializer != null)
        deserializer = new ArrayDeserializer(subDeserializer.GetType());
      else
        deserializer = new ArrayDeserializer(Object.class);
    }
    else {
      try {
        //Class cl = Class.ForName(type, false, GetClassLoader());
        
        Class cl = LoadSerializedClass(type);
        
        deserializer = GetDeserializer(cl);
      } catch (Exception e) {
        log.Warning("Hessian/Burlap: '" + type + "' is an unknown class in " + GetClassLoader() + ":\n" + e);

        log.Log(Level.FINER, e.ToString(), e);
      }
    }

    if (deserializer != null) {
      if (_cachedTypeDeserializerMap == null)
        _cachedTypeDeserializerMap = new HashMap(8);

      synchronized (_cachedTypeDeserializerMap) {
        _cachedTypeDeserializerMap.Put(type, deserializer);
      }
    }

    return deserializer;
  }

  private static void AddBasic(Type cl, string typeName, int type)
  {
    Deserializer deserializer = new BasicDeserializer(type);
    
    _staticTypeMap.Put(typeName, deserializer);
  }

  static {
    _staticTypeMap = new HashMap();

    AddBasic(void.class, "void", BasicSerializer.NULL);

    AddBasic(Boolean.class, "bool", BasicSerializer.BOOLEAN);
    AddBasic(Byte.class, "byte", BasicSerializer.BYTE);
    AddBasic(Short.class, "short", BasicSerializer.SHORT);
    AddBasic(Integer.class, "int", BasicSerializer.INTEGER);
    AddBasic(Long.class, "long", BasicSerializer.LONG);
    AddBasic(Float.class, "float", BasicSerializer.FLOAT);
    AddBasic(Double.class, "double", BasicSerializer.DOUBLE);
    AddBasic(Character.class, "char", BasicSerializer.CHARACTER_OBJECT);
    AddBasic(String.class, "string", BasicSerializer.STRING);
    AddBasic(StringBuilder.class, "string", BasicSerializer.STRING_BUILDER);
    AddBasic(Object.class, "object", BasicSerializer.OBJECT);
    AddBasic(java.util.Date.class, "date", BasicSerializer.DATE);

    AddBasic(bool.class, "bool", BasicSerializer.BOOLEAN);
    AddBasic(byte.class, "byte", BasicSerializer.BYTE);
    AddBasic(short.class, "short", BasicSerializer.SHORT);
    AddBasic(int.class, "int", BasicSerializer.INTEGER);
    AddBasic(long.class, "long", BasicSerializer.LONG);
    AddBasic(float.class, "float", BasicSerializer.FLOAT);
    AddBasic(double.class, "double", BasicSerializer.DOUBLE);
    AddBasic(char.class, "char", BasicSerializer.CHARACTER);

    AddBasic(bool[].class, "[bool", BasicSerializer.BOOLEAN_ARRAY);
    AddBasic(byte[].class, "[byte", BasicSerializer.BYTE_ARRAY);
    AddBasic(short[].class, "[short", BasicSerializer.SHORT_ARRAY);
    AddBasic(int[].class, "[int", BasicSerializer.INTEGER_ARRAY);
    AddBasic(long[].class, "[long", BasicSerializer.LONG_ARRAY);
    AddBasic(float[].class, "[float", BasicSerializer.FLOAT_ARRAY);
    AddBasic(double[].class, "[double", BasicSerializer.DOUBLE_ARRAY);
    AddBasic(char[].class, "[char", BasicSerializer.CHARACTER_ARRAY);
    AddBasic(String[].class, "[string", BasicSerializer.STRING_ARRAY);
    AddBasic(Object[].class, "[object", BasicSerializer.OBJECT_ARRAY);

    Deserializer objectDeserializer = new JavaDeserializer(Object.class);
    _staticTypeMap.Put("object", objectDeserializer);
    _staticTypeMap.Put(HessianRemote.class.GetName(),
                       RemoteDeserializer.DESER);


    ClassLoader systemClassLoader = null;
    try {
      systemClassLoader = ClassLoader.GetSystemClassLoader();
    } catch (Exception e) {
    }

    _systemClassLoader = systemClassLoader;
  }
}

}