using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{










/// <summary>
/// Input stream for Hessian requests.
///
/// <para/>HessianInput is unbuffered, so any client needs to provide
/// its own buffering.
///
/// <code>
/// InputStream is = ...; // from http connection
/// HessianInput in = new HessianInput(is);
/// string value;
///
/// in.StartReply();         // read reply header
/// value = in.ReadString(); // read string value
/// in.CompleteReply();      // read reply footer
/// </code>
/// </summary>
public class HessianInput : AbstractHessianInput {
  private static int END_OF_DATA = -2;

  private static Field _detailMessageField;
  
  // factory for deserializing objects in the input stream
  protected SerializerFactory _serializerFactory;
  
  protected ArrayList _refs;
  
  // the underlying input stream
  private InputStream _is;
  // a peek character
  protected int _peek = -1;
  
  // the method for a call
  private string _method;

  private Reader _chunkReader;
  private InputStream _chunkInputStream;

  private Throwable _replyFault;

  private StringBuffer _sbuf = new StringBuffer();
  
  // true if this is the last chunk
  private bool _isLastChunk;
  // the chunk length
  private int _chunkLength;

  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianInput()
  {
  }
  
  /// <summary>
  /// Creates a new Hessian input stream, initialized with an
  /// underlying input stream.
  ///
  /// <param name="is">the underlying input stream.</param>
  /// </summary>
  public HessianInput(InputStream is)
  {
    Init(is);
  }

  /// <summary>
  /// Sets the serializer factory.
  /// </summary>
  public void SetSerializerFactory(SerializerFactory factory)
  {
    _serializerFactory = factory;
  }

  /// <summary>
  /// Gets the serializer factory.
  /// </summary>
  public SerializerFactory GetSerializerFactory()
  {
    return _serializerFactory;
  }

  /// <summary>
  /// Initialize the hessian stream with the underlying input stream.
  /// </summary>
  public void Init(InputStream is)
  {
    _is = is;
    _method = null;
    _isLastChunk = true;
    _chunkLength = 0;
    _peek = -1;
    _refs = null;
    _replyFault = null;

    if (_serializerFactory == null)
      _serializerFactory = new SerializerFactory();
  }

  /// <summary>
  /// Returns the calls method
  /// </summary>
  public string GetMethod()
  {
    return _method;
  }

  /// <summary>
  /// Returns any reply fault.
  /// </summary>
  public Throwable GetReplyFault()
  {
    return _replyFault;
  }

  /// <summary>
  /// Starts reading the call
  ///
  /// <code>
  /// c major minor
  /// </code>
  /// </summary>
  public int ReadCall()
      {
    int tag = Read();
    
    if (tag != 'c')
      throw Error("expected hessian call ('c') at " + CodeName(tag));

    int major = Read();
    int minor = Read();

    return (major << 16) + minor;
  }

  /// <summary>
  /// For backward compatibility with HessianSkeleton
  /// </summary>
  public void SkipOptionalCall()
      {
    int tag = Read();

    if (tag == 'c') {
      Read();
      Read();
    }
    else
      _peek = tag;
  }

  /// <summary>
  /// Starts reading the call
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// m b16 b8 method
  /// </code>
  /// </summary>
  public string ReadMethod()
      {
    int tag = Read();
    
    if (tag != 'm')
      throw Error("expected hessian method ('m') at " + CodeName(tag));
    int d1 = Read();
    int d2 = Read();

    _isLastChunk = true;
    _chunkLength = d1/// 256 + d2;
    _sbuf.SetLength(0);
    int ch;
    while ((ch = ParseChar()) >= 0)
      _sbuf.Append((char) ch);
    
    _method = _sbuf.ToString();

    return _method;
  }

  /// <summary>
  /// Starts reading the call, including the headers.
  ///
  /// <para/>The call expects the following protocol data
  ///
  /// <code>
  /// c major minor
  /// m b16 b8 method
  /// </code>
  /// </summary>
  public void StartCall()
      {
    ReadCall();

    while (ReadHeader() != null) {
      ReadObject();
    }

    ReadMethod();
  }

  /// <summary>
  /// Completes reading the call
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// z
  /// </code>
  /// </summary>
  public void CompleteCall()
      {
    int tag = Read();

    if (tag == 'z') {
    }
    else
      throw Error("expected end of call ('z') at " + CodeName(tag) + ".  Check method arguments and ensure method overloading is enabled if necessary");
  }

  /// <summary>
  /// Reads a reply as an object.
  /// If the reply has a fault, exception.
  /// </summary>
  public object ReadReply(Class expectedClass)
  {
    int tag = Read();
    
    if (tag != 'r')
      Error("expected hessian reply at " + CodeName(tag));

    int major = Read();
    int minor = Read();

    tag = Read();
    if (tag == 'f')
      throw PrepareFault();
    else {
      _peek = tag;
    
      object value = ReadObject(expectedClass);

      CompleteValueReply();

      return value;
    }
  }

  /// <summary>
  /// Starts reading the reply
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// r
  /// </code>
  /// </summary>
  public void StartReply()
  {
    int tag = Read();
    
    if (tag != 'r')
      Error("expected hessian reply at " + CodeName(tag));

    int major = Read();
    int minor = Read();

    StartReplyBody();
  }

  public void StartReplyBody()
  {
    int tag = Read();
    
    if (tag == 'f')
      throw PrepareFault();
    else
      _peek = tag;
  }

  /// <summary>
  /// Prepares the fault.
  /// </summary>
  private Throwable PrepareFault()
      {
    HashMap fault = ReadFault();

    object detail = fault.Get("detail");
    string message = (String) fault.Get("message");

    if (detail instanceof Throwable) {
      _replyFault = (Throwable) detail;
      
      if (message != null && _detailMessageField != null) {
        try {
          _detailMessageField.Set(_replyFault, message);
        } catch (Throwable e) {
        }
      }

      return _replyFault;
    }

    else {
      string code = (String) fault.Get("code");
        
      _replyFault = new HessianServiceException(message, code, detail);

      return _replyFault;
    }
  }

  /// <summary>
  /// Completes reading the call
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// z
  /// </code>
  /// </summary>
  public void CompleteReply()
      {
    int tag = Read();
    
    if (tag != 'z')
      Error("expected end of reply at " + CodeName(tag));
  }

  /// <summary>
  /// Completes reading the call
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// z
  /// </code>
  /// </summary>
  public void CompleteValueReply()
      {
    int tag = Read();
    
    if (tag != 'z')
      Error("expected end of reply at " + CodeName(tag));
  }

  /// <summary>
  /// Reads a header, returning null if there are no headers.
  ///
  /// <code>
  /// H b16 b8 value
  /// </code>
  /// </summary>
  public string ReadHeader()
      {
    int tag = Read();

    if (tag == 'H') {
      _isLastChunk = true;
      _chunkLength = (Read() << 8) + Read();

      _sbuf.SetLength(0);
      int ch;
      while ((ch = ParseChar()) >= 0)
        _sbuf.Append((char) ch);

      return _sbuf.ToString();
    }

    _peek = tag;

    return null;
  }

  /// <summary>
  /// Reads a null
  ///
  /// <code>
  /// N
  /// </code>
  /// </summary>
  public void ReadNull()
      {
    int tag = Read();

    switch (tag) {
    case 'N': return;
      
    default:
      throw Expect("null", tag);
    }
  }

  /// <summary>
  /// Reads a bool
  ///
  /// <code>
  /// T
  /// F
  /// </code>
  /// </summary>
  public bool ReadBoolean()
      {
    int tag = Read();

    switch (tag) {
    case 'T': return true;
    case 'F': return false;
    case 'I': return ParseInt() == 0;
    case 'L': return ParseLong() == 0;
    case 'D': return ParseDouble() == 0.0;
    case 'N': return false;
      
    default:
      throw Expect("bool", tag);
    }
  }

  /// <summary>
  /// Reads a byte
  ///
  /// <code>
  /// I b32 b24 b16 b8
  /// </code>
  /// </summary>
  /*
  public byte ReadByte()
      {
    return (byte) ReadInt();
  }
 /// </summary>

  /// <summary>
  /// Reads a short
  ///
  /// <code>
  /// I b32 b24 b16 b8
  /// </code>
  /// </summary>
  public short ReadShort()
      {
    return (short) ReadInt();
  }

  /// <summary>
  /// Reads an integer
  ///
  /// <code>
  /// I b32 b24 b16 b8
  /// </code>
  /// </summary>
  public int ReadInt()
      {
    int tag = Read();

    switch (tag) {
    case 'T': return 1;
    case 'F': return 0;
    case 'I': return ParseInt();
    case 'L': return (int) ParseLong();
    case 'D': return (int) ParseDouble();
      
    default:
      throw Expect("int", tag);
    }
  }

  /// <summary>
  /// Reads a long
  ///
  /// <code>
  /// L b64 b56 b48 b40 b32 b24 b16 b8
  /// </code>
  /// </summary>
  public long ReadLong()
      {
    int tag = Read();

    switch (tag) {
    case 'T': return 1;
    case 'F': return 0;
    case 'I': return ParseInt();
    case 'L': return ParseLong();
    case 'D': return (long) ParseDouble();
      
    default:
      throw Expect("long", tag);
    }
  }

  /// <summary>
  /// Reads a float
  ///
  /// <code>
  /// D b64 b56 b48 b40 b32 b24 b16 b8
  /// </code>
  /// </summary>
  public float ReadFloat()
      {
    return (float) ReadDouble();
  }

  /// <summary>
  /// Reads a double
  ///
  /// <code>
  /// D b64 b56 b48 b40 b32 b24 b16 b8
  /// </code>
  /// </summary>
  public double ReadDouble()
      {
    int tag = Read();

    switch (tag) {
    case 'T': return 1;
    case 'F': return 0;
    case 'I': return ParseInt();
    case 'L': return (double) ParseLong();
    case 'D': return ParseDouble();
      
    default:
      throw Expect("long", tag);
    }
  }

  /// <summary>
  /// Reads a date.
  ///
  /// <code>
  /// T b64 b56 b48 b40 b32 b24 b16 b8
  /// </code>
  /// </summary>
  public long ReadUTCDate()
      {
    int tag = Read();

    if (tag != 'd')
      throw Error("expected date at " + CodeName(tag));

    long b64 = Read();
    long b56 = Read();
    long b48 = Read();
    long b40 = Read();
    long b32 = Read();
    long b24 = Read();
    long b16 = Read();
    long b8 = Read();

    return ((b64 << 56) +
            (b56 << 48) +
            (b48 << 40) +
            (b40 << 32) +
            (b32 << 24) +
            (b24 << 16) +
            (b16 << 8) +
            b8);
  }

  /// <summary>
  /// Reads a byte from the stream.
  /// </summary>
  public int ReadChar()
      {
    if (_chunkLength > 0) {
      _chunkLength--;
      if (_chunkLength == 0 && _isLastChunk)
        _chunkLength = END_OF_DATA;

      int ch = ParseUTF8Char();
      return ch;
    }
    else if (_chunkLength == END_OF_DATA) {
      _chunkLength = 0;
      return -1;
    }
    
    int tag = Read();

    switch (tag) {
    case 'N':
      return -1;

    case 'S':
    case 's':
    case 'X':
    case 'x':
      _isLastChunk = tag == 'S' || tag == 'X';
      _chunkLength = (Read() << 8) + Read();

      _chunkLength--;
      int value = ParseUTF8Char();

      // special code so successive read byte won't
      // be read as a single object.
      if (_chunkLength == 0 && _isLastChunk)
        _chunkLength = END_OF_DATA;

      return value;
      
    default:
      throw new IOException("expected 'S' at " + (char) tag);
    }
  }

  /// <summary>
  /// Reads a byte array from the stream.
  /// </summary>
  public int ReadString(char[] buffer, int offset, int length)
      {
    int readLength = 0;

    if (_chunkLength == END_OF_DATA) {
      _chunkLength = 0;
      return -1;
    }
    else if (_chunkLength == 0) {
      int tag = Read();

      switch (tag) {
      case 'N':
        return -1;
      
      case 'S':
      case 's':
      case 'X':
      case 'x':
        _isLastChunk = tag == 'S' || tag == 'X';
        _chunkLength = (Read() << 8) + Read();
        break;

      default:
        throw new IOException("expected 'S' at " + (char) tag);
      }
    }

    while (length > 0) {
      if (_chunkLength > 0) {
        buffer[offset++] = (char) ParseUTF8Char();
        _chunkLength--;
        length--;
        readLength++;
      }
      else if (_isLastChunk) {
        if (readLength == 0)
          return -1;
        else {
          _chunkLength = END_OF_DATA;
          return readLength;
        }
      }
      else {
        int tag = Read();

        switch (tag) {
        case 'S':
        case 's':
        case 'X':
        case 'x':
          _isLastChunk = tag == 'S' || tag == 'X';
          _chunkLength = (Read() << 8) + Read();
          break;
      
        default:
          throw new IOException("expected 'S' at " + (char) tag);
        }
      }
    }
    
    if (readLength == 0)
      return -1;
    else if (_chunkLength > 0 || ! _isLastChunk)
      return readLength;
    else {
      _chunkLength = END_OF_DATA;
      return readLength;
    }
  }

  /// <summary>
  /// Reads a string
  ///
  /// <code>
  /// S b16 b8 string value
  /// </code>
  /// </summary>
  public string ReadString()
      {
    int tag = Read();

    switch (tag) {
    case 'N':
      return null;

    case 'I':
      return String.ValueOf(ParseInt());
    case 'L':
      return String.ValueOf(ParseLong());
    case 'D':
      return String.ValueOf(ParseDouble());

    case 'S':
    case 's':
    case 'X':
    case 'x':
      _isLastChunk = tag == 'S' || tag == 'X';
      _chunkLength = (Read() << 8) + Read();

      _sbuf.SetLength(0);
      int ch;

      while ((ch = ParseChar()) >= 0)
        _sbuf.Append((char) ch);

      return _sbuf.ToString();

    default:
      throw Expect("string", tag);
    }
  }

  /// <summary>
  /// Reads an XML node.
  ///
  /// <code>
  /// S b16 b8 string value
  /// </code>
  /// </summary>
  public org.w3c.dom.Node ReadNode()
      {
    int tag = Read();

    switch (tag) {
    case 'N':
      return null;

    case 'S':
    case 's':
    case 'X':
    case 'x':
      _isLastChunk = tag == 'S' || tag == 'X';
      _chunkLength = (Read() << 8) + Read();

      throw Error("Can't handle string in this context");

    default:
      throw Expect("string", tag);
    }
  }

  /// <summary>
  /// Reads a byte array
  ///
  /// <code>
  /// B b16 b8 data value
  /// </code>
  /// </summary>
  public byte[] ReadBytes()
      {
    int tag = Read();

    switch (tag) {
    case 'N':
      return null;

    case 'B':
    case 'b':
      _isLastChunk = tag == 'B';
      _chunkLength = (Read() << 8) + Read();

      ByteArrayOutputStream bos = new ByteArrayOutputStream();

      int data;
      while ((data = ParseByte()) >= 0)
        bos.Write(data);

      return bos.ToByteArray();
      
    default:
      throw Expect("bytes", tag);
    }
  }

  /// <summary>
  /// Reads a byte from the stream.
  /// </summary>
  public int ReadByte()
      {
    if (_chunkLength > 0) {
      _chunkLength--;
      if (_chunkLength == 0 && _isLastChunk)
        _chunkLength = END_OF_DATA;

      return Read();
    }
    else if (_chunkLength == END_OF_DATA) {
      _chunkLength = 0;
      return -1;
    }
    
    int tag = Read();

    switch (tag) {
    case 'N':
      return -1;

    case 'B':
    case 'b':
      _isLastChunk = tag == 'B';
      _chunkLength = (Read() << 8) + Read();

      int value = ParseByte();

      // special code so successive read byte won't
      // be read as a single object.
      if (_chunkLength == 0 && _isLastChunk)
        _chunkLength = END_OF_DATA;

      return value;
      
    default:
      throw new IOException("expected 'B' at " + (char) tag);
    }
  }

  /// <summary>
  /// Reads a byte array from the stream.
  /// </summary>
  public int ReadBytes(byte[] buffer, int offset, int length)
      {
    int readLength = 0;

    if (_chunkLength == END_OF_DATA) {
      _chunkLength = 0;
      return -1;
    }
    else if (_chunkLength == 0) {
      int tag = Read();

      switch (tag) {
      case 'N':
        return -1;
      
      case 'B':
      case 'b':
        _isLastChunk = tag == 'B';
        _chunkLength = (Read() << 8) + Read();
        break;
      
      default:
        throw new IOException("expected 'B' at " + (char) tag);
      }
    }

    while (length > 0) {
      if (_chunkLength > 0) {
        buffer[offset++] = (byte) Read();
        _chunkLength--;
        length--;
        readLength++;
      }
      else if (_isLastChunk) {
        if (readLength == 0)
          return -1;
        else {
          _chunkLength = END_OF_DATA;
          return readLength;
        }
      }
      else {
        int tag = Read();

        switch (tag) {
        case 'B':
        case 'b':
          _isLastChunk = tag == 'B';
          _chunkLength = (Read() << 8) + Read();
          break;
      
        default:
          throw new IOException("expected 'B' at " + (char) tag);
        }
      }
    }
    
    if (readLength == 0)
      return -1;
    else if (_chunkLength > 0 || ! _isLastChunk)
      return readLength;
    else {
      _chunkLength = END_OF_DATA;
      return readLength;
    }
  }

  /// <summary>
  /// Reads a fault.
  /// </summary>
  private HashMap ReadFault()
      {
    HashMap map = new HashMap();

    int code = Read();
    for (; code > 0 && code != 'z'; code = Read()) {
      _peek = code;
      
      object key = ReadObject();
      object value = ReadObject();

      if (key != null && value != null)
        map.Put(key, value);
    }

    if (code != 'z')
      throw Expect("fault", code);

    return map;
  }

  /// <summary>
  /// Reads an object from the input stream with an expected type.
  /// </summary>
  public object ReadObject(Class cl)
      {
    if (cl == null || cl == Object.class)
      return ReadObject();
    
    int tag = Read();
    
    switch (tag) {
    case 'N':
      return null;

    case 'M':
    {
      string type = ReadType();

      // hessian/3386
      if ("".Equals(type)) {
        Deserializer reader;
        reader = _serializerFactory.GetDeserializer(cl);

        return reader.ReadMap(this);
      }
      else {
        Deserializer reader;
        reader = _serializerFactory.GetObjectDeserializer(type);

        return reader.ReadMap(this);
      }
    }

    case 'V':
    {
      string type = ReadType();
      int length = ReadLength();
      
      Deserializer reader;
      reader = _serializerFactory.GetObjectDeserializer(type);
      
      if (cl != reader.GetType() && cl.IsAssignableFrom(reader.GetType()))
        return reader.ReadList(this, length);

      reader = _serializerFactory.GetDeserializer(cl);

      object v = reader.ReadList(this, length);

      return v;
    }

    case 'R':
    {
      int ref = ParseInt();

      return _refs.Get(ref);
    }

    case 'r':
    {
      string type = ReadType();
      string url = ReadString();

      return ResolveRemote(type, url);
    }
    }

    _peek = tag;

    // hessian/332i vs hessian/3406
    //return ReadObject();
    
    object value = _serializerFactory.GetDeserializer(cl).ReadObject(this);

    return value;
  }
  
  /// <summary>
  /// Reads an arbitrary object from the input stream when the type
  /// is unknown.
  /// </summary>
  public object ReadObject()
      {
    int tag = Read();

    switch (tag) {
    case 'N':
      return null;
      
    case 'T':
      return Boolean.ValueOf(true);
      
    case 'F':
      return Boolean.ValueOf(false);
      
    case 'I':
      return Integer.ValueOf(ParseInt());
    
    case 'L':
      return Long.ValueOf(ParseLong());
    
    case 'D':
      return Double.ValueOf(ParseDouble());
    
    case 'd':
      return new Date(ParseLong());
    
    case 'x':
    case 'X': {
      _isLastChunk = tag == 'X';
      _chunkLength = (Read() << 8) + Read();

      return ParseXML();
    }

    case 's':
    case 'S': {
      _isLastChunk = tag == 'S';
      _chunkLength = (Read() << 8) + Read();

      int data;
      _sbuf.SetLength(0);
      
      while ((data = ParseChar()) >= 0)
        _sbuf.Append((char) data);

      return _sbuf.ToString();
    }

    case 'b':
    case 'B': {
      _isLastChunk = tag == 'B';
      _chunkLength = (Read() << 8) + Read();

      int data;
      ByteArrayOutputStream bos = new ByteArrayOutputStream();
      
      while ((data = ParseByte()) >= 0)
        bos.Write(data);

      return bos.ToByteArray();
    }

    case 'V': {
      string type = ReadType();
      int length = ReadLength();

      return _serializerFactory.ReadList(this, length, type);
    }

    case 'M': {
      string type = ReadType();

      return _serializerFactory.ReadMap(this, type);
    }

    case 'R': {
      int ref = ParseInt();

      return _refs.Get(ref);
    }

    case 'r': {
      string type = ReadType();
      string url = ReadString();

      return ResolveRemote(type, url);
    }

    default:
      throw Error("unknown code for readobject at " + CodeName(tag));
    }
  }

  /// <summary>
  /// Reads a remote object.
  /// </summary>
  public object ReadRemote()
      {
    string type = ReadType();
    string url = ReadString();

    return ResolveRemote(type, url);
  }

  /// <summary>
  /// Reads a reference.
  /// </summary>
  public object ReadRef()
      {
    return _refs.Get(ParseInt());
  }

  /// <summary>
  /// Reads the start of a list.
  /// </summary>
  public int ReadListStart()
      {
    return Read();
  }

  /// <summary>
  /// Reads the start of a list.
  /// </summary>
  public int ReadMapStart()
      {
    return Read();
  }

  /// <summary>
  /// Returns true if this is the end of a list or a map.
  /// </summary>
  public bool IsEnd()
      {
    int code = Read();

    _peek = code;

    return (code < 0 || code == 'z');
  }

  /// <summary>
  /// Reads the end byte.
  /// </summary>
  public void ReadEnd()
      {
    int code = Read();

    if (code != 'z')
      throw Error("unknown code at " + CodeName(code));
  }

  /// <summary>
  /// Reads the end byte.
  /// </summary>
  public void ReadMapEnd()
      {
    int code = Read();

    if (code != 'z')
      throw Error("expected end of map ('z') at " + CodeName(code));
  }

  /// <summary>
  /// Reads the end byte.
  /// </summary>
  public void ReadListEnd()
      {
    int code = Read();

    if (code != 'z')
      throw Error("expected end of list ('z') at " + CodeName(code));
  }

  /// <summary>
  /// Adds a list/map reference.
  /// </summary>
  public int AddRef(object ref)
  {
    if (_refs == null)
      _refs = new ArrayList();
    
    _refs.Add(ref);

    return _refs.Size() - 1;
  }

  /// <summary>
  /// Adds a list/map reference.
  /// </summary>
  public void SetRef(int i, object ref)
  {
    _refs.Set(i, ref);
  }

  /// <summary>
  /// Resets the references for streaming.
  /// </summary>
  public void ResetReferences()
  {
    if (_refs != null)
      _refs.Clear();
  }

  /// <summary>
  /// Resolves a remote object.
  /// </summary>
  public object ResolveRemote(string type, string url)
      {
    HessianRemoteResolver resolver = GetRemoteResolver();

    if (resolver != null)
      return resolver.Lookup(type, url);
    else
      return new HessianRemote(type, url);
  }

  /// <summary>
  /// Parses a type from the stream.
  ///
  /// <code>
  /// t b16 b8
  /// </code>
  /// </summary>
  public string ReadType()
      {
    int code = Read();

    if (code != 't') {
      _peek = code;
      return "";
    }

    _isLastChunk = true;
    _chunkLength = (Read() << 8) + Read();

    _sbuf.SetLength(0);
    int ch;
    while ((ch = ParseChar()) >= 0)
      _sbuf.Append((char) ch);

    return _sbuf.ToString();
  }

  /// <summary>
  /// Parses the length for an array
  ///
  /// <code>
  /// l b32 b24 b16 b8
  /// </code>
  /// </summary>
  public int ReadLength()
      {
    int code = Read();

    if (code != 'l') {
      _peek = code;
      return -1;
    }

    return ParseInt();
  }

  /// <summary>
  /// Parses a 32-bit integer value from the stream.
  ///
  /// <code>
  /// b32 b24 b16 b8
  /// </code>
  /// </summary>
  private int ParseInt()
      {
    int b32 = Read();
    int b24 = Read();
    int b16 = Read();
    int b8 = Read();

    return (b32 << 24) + (b24 << 16) + (b16 << 8) + b8;
  }

  /// <summary>
  /// Parses a 64-bit long value from the stream.
  ///
  /// <code>
  /// b64 b56 b48 b40 b32 b24 b16 b8
  /// </code>
  /// </summary>
  private long ParseLong()
      {
    long b64 = Read();
    long b56 = Read();
    long b48 = Read();
    long b40 = Read();
    long b32 = Read();
    long b24 = Read();
    long b16 = Read();
    long b8 = Read();

    return ((b64 << 56) +
            (b56 << 48) +
            (b48 << 40) +
            (b40 << 32) +
            (b32 << 24) +
            (b24 << 16) +
            (b16 << 8) +
            b8);
  }
  
  /// <summary>
  /// Parses a 64-bit double value from the stream.
  ///
  /// <code>
  /// b64 b56 b48 b40 b32 b24 b16 b8
  /// </code>
  /// </summary>
  private double ParseDouble()
      {
    long b64 = Read();
    long b56 = Read();
    long b48 = Read();
    long b40 = Read();
    long b32 = Read();
    long b24 = Read();
    long b16 = Read();
    long b8 = Read();

    long bits = ((b64 << 56) +
                 (b56 << 48) +
                 (b48 << 40) +
                 (b40 << 32) +
                 (b32 << 24) +
                 (b24 << 16) +
                 (b16 << 8) +
                 b8);
  
    return Double.LongBitsToDouble(bits);
  }

  org.w3c.dom.Node ParseXML()
      {
    throw new NotSupportedException();
  }
  
  /// <summary>
  /// Reads a character from the underlying stream.
  /// </summary>
  private int ParseChar()
      {
    while (_chunkLength <= 0) {
      if (_isLastChunk)
        return -1;

      int code = Read();

      switch (code) {
      case 's':
      case 'x':
        _isLastChunk = false;

        _chunkLength = (Read() << 8) + Read();
        break;
        
      case 'S':
      case 'X':
        _isLastChunk = true;

        _chunkLength = (Read() << 8) + Read();
        break;

      default:
        throw Expect("string", code);
      }

    }

    _chunkLength--;

    return ParseUTF8Char();
  }

  /// <summary>
  /// Parses a single UTF8 character.
  /// </summary>
  private int ParseUTF8Char()
      {
    int ch = Read();

    if (ch < 0x80)
      return ch;
    else if ((ch & 0xe0) == 0xc0) {
      int ch1 = Read();
      int v = ((ch & 0x1f) << 6) + (ch1 & 0x3f);

      return v;
    }
    else if ((ch & 0xf0) == 0xe0) {
      int ch1 = Read();
      int ch2 = Read();
      int v = ((ch & 0x0f) << 12) + ((ch1 & 0x3f) << 6) + (ch2 & 0x3f);

      return v;
    }
    else
      throw Error("bad utf-8 encoding at " + CodeName(ch));
  }
  
  /// <summary>
  /// Reads a byte from the underlying stream.
  /// </summary>
  private int ParseByte()
      {
    while (_chunkLength <= 0) {
      if (_isLastChunk) {
        return -1;
      }

      int code = Read();

      switch (code) {
      case 'b':
        _isLastChunk = false;

        _chunkLength = (Read() << 8) + Read();
        break;
        
      case 'B':
        _isLastChunk = true;

        _chunkLength = (Read() << 8) + Read();
        break;

      default:
        throw Expect("byte[]", code);
      }
    }

    _chunkLength--;

    return Read();
  }

  /// <summary>
  /// Reads bytes based on an input stream.
  /// </summary>
  public InputStream ReadInputStream()
      {
    int tag = Read();

    switch (tag) {
    case 'N':
      return null;

    case 'B':
    case 'b':
      _isLastChunk = tag == 'B';
      _chunkLength = (Read() << 8) + Read();
      break;
      
    default:
      throw Expect("inputStream", tag);
    }
    
    return new InputStream() {
        bool _isClosed = false;

        public int Read()
                  {
          if (_isClosed || _is == null)
            return -1;

          int ch = ParseByte();
          if (ch < 0)
            _isClosed = true;

          return ch;
        }

        public int Read(byte[] buffer, int offset, int length)
                  {
          if (_isClosed || _is == null)
            return -1;

          int len = HessianInput.this.Read(buffer, offset, length);
          if (len < 0)
            _isClosed = true;

          return len;
        }

        public void Close()
                  {
          while (Read() >= 0) {
          }

          _isClosed = true;
        }
      };
  }
  
  /// <summary>
  /// Reads bytes from the underlying stream.
  /// </summary>
  int Read(byte[] buffer, int offset, int length)
      {
    int readLength = 0;
    
    while (length > 0) {
      while (_chunkLength <= 0) {
        if (_isLastChunk)
          return readLength == 0 ? -1 : readLength;

        int code = Read();

        switch (code) {
        case 'b':
          _isLastChunk = false;

          _chunkLength = (Read() << 8) + Read();
          break;
        
        case 'B':
          _isLastChunk = true;

          _chunkLength = (Read() << 8) + Read();
          break;

        default:
          throw Expect("byte[]", code);
        }
      }

      int sublen = _chunkLength;
      if (length < sublen)
        sublen = length;

      sublen = _is.Read(buffer, offset, sublen);
      offset += sublen;
      readLength += sublen;
      length -= sublen;
      _chunkLength -= sublen;
    }

    return readLength;
  }

  readonly int Read()
      {
    if (_peek >= 0) {
      int value = _peek;
      _peek = -1;
      return value;
    }

    int ch = _is.Read();
      
    return ch;
  }

  public void Close()
  {
    _is = null;
  }

  public Reader GetReader()
  {
    return null;
  }

  protected IOException Expect(string expect, int ch)
  {
    return Error("expected " + expect + " at " + CodeName(ch));
  }

  protected string CodeName(int ch)
  {
    if (ch < 0)
      return "end of file";
    else
      return "0x" + Integer.ToHexString(ch & 0xff) + " (" + (char) + ch + ")";
  }
  
  protected IOException Error(string message)
  {
    if (_method != null)
      return new HessianProtocolException(_method + ": " + message);
    else
      return new HessianProtocolException(message);
  }

  static {
    try {
      _detailMessageField = Throwable.class.GetDeclaredField("detailMessage");
      _detailMessageField.SetAccessible(true);
    } catch (Throwable e) {
    }
  }
}

}