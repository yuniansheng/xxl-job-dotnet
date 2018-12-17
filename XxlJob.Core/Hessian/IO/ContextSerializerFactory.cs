using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{


















/// <summary>
/// The classloader-specific Factory for returning serialization
/// </summary>
public class ContextSerializerFactory
{
  private static readonly Logger log
    = Logger.GetLogger(ContextSerializerFactory.class.GetName());

  private static IDeserializer OBJECT_DESERIALIZER
    = new BasicDeserializer(BasicDeserializer.OBJECT);

  private static readonly WeakHashMap<ClassLoader,SoftReference<ContextSerializerFactory>>
    _contextRefMap
    = new WeakHashMap<ClassLoader,SoftReference<ContextSerializerFactory>>();

  private static readonly ClassLoader _systemClassLoader;

  private static HashMap<String,ISerializer> _staticSerializerMap;
  private static HashMap<String,IDeserializer> _staticDeserializerMap;
  private static HashMap _staticClassNameMap;

  private ContextSerializerFactory _parent;
  private WeakReference<ClassLoader> _loaderRef;

  private readonly HashSet<String> _serializerFiles = new HashSet<String>();
  private readonly HashSet<String> _deserializerFiles = new HashSet<String>();

  private readonly HashMap<String,ISerializer> _serializerClassMap
    = new HashMap<String,ISerializer>();

  private readonly ConcurrentHashMap<String,ISerializer> _customSerializerMap
    = new ConcurrentHashMap<String,ISerializer>();

  private readonly HashMap<Type,ISerializer> _serializerInterfaceMap
    = new HashMap<Type,ISerializer>();

  private readonly HashMap<String,IDeserializer> _deserializerClassMap
    = new HashMap<String,IDeserializer>();

  private readonly HashMap<String,IDeserializer> _deserializerClassNameMap
    = new HashMap<String,IDeserializer>();

  private readonly ConcurrentHashMap<String,IDeserializer> _customDeserializerMap
    = new ConcurrentHashMap<String,IDeserializer>();

  private readonly HashMap<Type,IDeserializer> _deserializerInterfaceMap
    = new HashMap<Type,IDeserializer>();

  public ContextSerializerFactory(ContextSerializerFactory parent,
                                  ClassLoader loader)
  {
    if (loader == null)
      loader = _systemClassLoader;

    _loaderRef = new WeakReference<ClassLoader>(loader);

    Init();
  }

  public static ContextSerializerFactory Create()
  {
    return Create(Thread.CurrentThread().GetContextClassLoader());
  }

  public static ContextSerializerFactory Create(ClassLoader loader)
  {
    synchronized (_contextRefMap) {
      SoftReference<ContextSerializerFactory> factoryRef
        = _contextRefMap.Get(loader);

      ContextSerializerFactory factory = null;

      if (factoryRef != null)
        factory = factoryRef.Get();

      if (factory == null) {
        ContextSerializerFactory parent = null;

        if (loader != null)
          parent = Create(loader.GetParent());

        factory = new ContextSerializerFactory(parent, loader);
        factoryRef = new SoftReference<ContextSerializerFactory>(factory);

        _contextRefMap.Put(loader, factoryRef);
      }

      return factory;
    }
  }

  public ClassLoader GetClassLoader()
  {
    WeakReference<ClassLoader> loaderRef = _loaderRef;
    
    if (loaderRef != null)
      return loaderRef.Get();
    else
      return null;
  }

  /// <summary>
  /// Returns the serializer for a given class.
  /// </summary>
  public ISerializer GetSerializer(string className)
  {
    ISerializer serializer = _serializerClassMap.Get(className);

    if (serializer == AbstractSerializer.NULL)
      return null;
    else
      return serializer;
  }

  /// <summary>
  /// Returns a custom serializer the class
  ///
  /// <param name="cl">the class of the object that needs to be serialized.</param>
  ///
  /// <returns>a serializer object for the serialization.</returns>
  /// </summary>
  public ISerializer GetCustomSerializer(Class cl)
  {
    ISerializer serializer = _customSerializerMap.Get(cl.GetName());

    if (serializer == AbstractSerializer.NULL)
      return null;
    else if (serializer != null)
      return serializer;

    try {
      Class serClass = Class.ForName(cl.GetName() + "HessianSerializer",
                                       false, cl.GetClassLoader());

      ISerializer ser = (ISerializer) serClass.NewInstance();

      _customSerializerMap.Put(cl.GetName(), ser);

      return ser;
    } catch (ClassNotFoundException e) {
      log.Log(Level.ALL, e.ToString(), e);
    } catch (Exception e) {
      throw new HessianException(e);
    }

    _customSerializerMap.Put(cl.GetName(), AbstractSerializer.NULL);

    return null;
  }

  /// <summary>
  /// Returns the deserializer for a given class.
  /// </summary>
  public IDeserializer GetDeserializer(string className)
  {
    IDeserializer deserializer = _deserializerClassMap.Get(className);

    if (deserializer == AbstractDeserializer.NULL)
      return null;
    else
      return deserializer;
  }

  /// <summary>
  /// Returns a custom deserializer the class
  ///
  /// <param name="cl">the class of the object that needs to be deserialized.</param>
  ///
  /// <returns>a deserializer object for the deserialization.</returns>
  /// </summary>
  public IDeserializer GetCustomDeserializer(Class cl)
  {
    IDeserializer deserializer = _customDeserializerMap.Get(cl.GetName());

    if (deserializer == AbstractDeserializer.NULL)
      return null;
    else if (deserializer != null)
      return deserializer;

    try {
      Class serClass = Class.ForName(cl.GetName() + "HessianDeserializer",
                                       false, cl.GetClassLoader());

      IDeserializer ser = (IDeserializer) serClass.NewInstance();

      _customDeserializerMap.Put(cl.GetName(), ser);

      return ser;
    } catch (ClassNotFoundException e) {
      log.Log(Level.ALL, e.ToString(), e);
    } catch (Exception e) {
      throw new HessianException(e);
    }

    _customDeserializerMap.Put(cl.GetName(), AbstractDeserializer.NULL);

    return null;
  }

  /// <summary>
  /// Initialize the factory
  /// </summary>
  private void Init()
  {
    if (_parent != null) {
      _serializerFiles.AddAll(_parent._serializerFiles);
      _deserializerFiles.AddAll(_parent._deserializerFiles);

      _serializerClassMap.PutAll(_parent._serializerClassMap);
      _deserializerClassMap.PutAll(_parent._deserializerClassMap);
    }

    if (_parent == null) {
      _serializerClassMap.PutAll(_staticSerializerMap);
      _deserializerClassMap.PutAll(_staticDeserializerMap);
      _deserializerClassNameMap.PutAll(_staticClassNameMap);
    }

    HashMap<Class,Class> classMap;

    classMap = new HashMap<Class,Class>();
    InitSerializerFiles("META-INF/hessian/serializers",
                        _serializerFiles,
                        classMap,
                        ISerializer.class);

    for (Map.Entry<Class,Class> entry : classMap.EntrySet()) {
      try {
        ISerializer ser = (ISerializer) entry.GetValue().NewInstance();

        if (entry.GetKey().IsInterface())
          _serializerInterfaceMap.Put(entry.GetKey(), ser);
        else
          _serializerClassMap.Put(entry.GetKey().GetName(), ser);
      } catch (Exception e) {
        throw new HessianException(e);
      }
    }

    classMap = new HashMap<Class,Class>();
    InitSerializerFiles("META-INF/hessian/deserializers",
                        _deserializerFiles,
                        classMap,
                        Deserializer.class);

    for (Map.Entry<Class,Class> entry : classMap.EntrySet()) {
      try {
        Deserializer ser = (Deserializer) entry.GetValue().NewInstance();

        if (entry.GetKey().IsInterface())
          _deserializerInterfaceMap.Put(entry.GetKey(), ser);
        else {
          _deserializerClassMap.Put(entry.GetKey().GetName(), ser);
        }
      } catch (Exception e) {
        throw new HessianException(e);
      }
    }
  }

  private void InitSerializerFiles(string fileName,
                                   HashSet<String> fileList,
                                   HashMap<Class,Class> classMap,
                                   Class type)
  {
    try {
      ClassLoader classLoader = GetClassLoader();
      
      // on systems with the security manager enabled, the system classloader
      // is null
      if (classLoader == null)
        return;

      Enumeration iter;
      
      iter = classLoader.GetResources(fileName);
      while (iter.HasMoreElements()) {
        URL url = (URL) iter.NextElement();

        if (fileList.Contains(url.ToString()))
          continue;

        fileList.Add(url.ToString());

        InputStream is = null;
        try {
          is = url.OpenStream();

          Properties props = new Properties();
          props.Load(is);

          for (Map.Entry entry : props.EntrySet()) {
            string apiName = (String) entry.GetKey();
            string serializerName = (String) entry.GetValue();

            Class apiClass = null;
            Class serializerClass = null;

            try {
              apiClass = Class.ForName(apiName, false, classLoader);
            } catch (ClassNotFoundException e) {
              log.Fine(url + ": " + apiName + " is not available in this context: " + GetClassLoader());
              continue;
            }

            try {
              serializerClass = Class.ForName(serializerName, false, classLoader);
            } catch (ClassNotFoundException e) {
              log.Fine(url + ": " + serializerName + " is not available in this context: " + GetClassLoader());
              continue;
            }

            if (! type.IsAssignableFrom(serializerClass))
              throw new HessianException(url + ": " + serializerClass.GetName() + " is invalid because it does not implement " + type.GetName());

            classMap.Put(apiClass, serializerClass);
          }
        } readonlyly {
          if (is != null)
            is.Close();
        }
      }
    } catch (RuntimeException e) {
      throw e;
    } catch (Exception e) {
      throw new HessianException(e);
    }
  }

  private static void AddBasic(Class cl, string typeName, int type)
  {
    _staticSerializerMap.Put(cl.GetName(), new BasicSerializer(type));

    Deserializer deserializer = new BasicDeserializer(type);
    _staticDeserializerMap.Put(cl.GetName(), deserializer);
    _staticClassNameMap.Put(typeName, deserializer);
  }

  static {
    _staticSerializerMap = new HashMap();
    _staticDeserializerMap = new HashMap();
    _staticClassNameMap = new HashMap();

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
    _staticSerializerMap.Put(byte[].class.GetName(), ByteArraySerializer.SER);
    AddBasic(short[].class, "[short", BasicSerializer.SHORT_ARRAY);
    AddBasic(int[].class, "[int", BasicSerializer.INTEGER_ARRAY);
    AddBasic(long[].class, "[long", BasicSerializer.LONG_ARRAY);
    AddBasic(float[].class, "[float", BasicSerializer.FLOAT_ARRAY);
    AddBasic(double[].class, "[double", BasicSerializer.DOUBLE_ARRAY);
    AddBasic(char[].class, "[char", BasicSerializer.CHARACTER_ARRAY);
    AddBasic(String[].class, "[string", BasicSerializer.STRING_ARRAY);
    AddBasic(Object[].class, "[object", BasicSerializer.OBJECT_ARRAY);

    Deserializer objectDeserializer = new JavaDeserializer(Object.class);
    _staticDeserializerMap.Put("object", objectDeserializer);
    _staticClassNameMap.Put("object", objectDeserializer);

    _staticSerializerMap.Put(Class.class.GetName(), new ClassSerializer());

    _staticDeserializerMap.Put(Number.class.GetName(), new BasicDeserializer(BasicSerializer.NUMBER));

    /*
    for (Class cl : new Class[] { BigDecimal.class, File.class, ObjectName.class }) {
      _staticSerializerMap.Put(cl, StringValueSerializer.SER);
      _staticDeserializerMap.Put(cl, new StringValueDeserializer(cl));
    }

    _staticSerializerMap.Put(ObjectName.class, StringValueSerializer.SER);
    try {
      _staticDeserializerMap.Put(ObjectName.class,
                           new StringValueDeserializer(ObjectName.class));
    } catch (Throwable e) {
    }
   /// </summary>
    
    _staticSerializerMap.Put(InetAddress.class.GetName(),
                             InetAddressSerializer.Create());

    _staticSerializerMap.Put(java.sql.Date.class.GetName(),
                             new SqlDateSerializer());
    _staticSerializerMap.Put(java.sql.Time.class.GetName(),
                             new SqlDateSerializer());
    _staticSerializerMap.Put(java.sql.Timestamp.class.GetName(),
                             new SqlDateSerializer());

    _staticDeserializerMap.Put(java.sql.Date.class.GetName(),
                               new SqlDateDeserializer(java.sql.Date.class));
    _staticDeserializerMap.Put(java.sql.Time.class.GetName(),
                               new SqlDateDeserializer(java.sql.Time.class));
    _staticDeserializerMap.Put(java.sql.Timestamp.class.GetName(),
                               new SqlDateDeserializer(java.sql.Timestamp.class));

    // hessian/3bb5
    _staticDeserializerMap.Put(StackTraceElement.class.GetName(),
                               new StackTraceElementDeserializer());

    ClassLoader systemClassLoader = null;
    try {
      systemClassLoader = ClassLoader.GetSystemClassLoader();
    } catch (Exception e) {
    }

    _systemClassLoader = systemClassLoader;
  }
}


}