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
public class Hessian2Input
  : AbstractHessianInput
  : Hessian2Constants
{
  private static readonly Logger log
    = Logger.GetLogger(Hessian2Input.class.GetName());
  
  private static readonly int END_OF_DATA = -2;

  private static Field _detailMessageField;

  private static readonly int SIZE = 1024;
  private static readonly int GAP = 16;
  
  // standard, unmodified factory for deserializing objects
  protected SerializerFactory _defaultSerializerFactory;
  // factory for deserializing objects in the input stream
  protected SerializerFactory _serializerFactory;

  private static bool _isCloseStreamOnClose;
  
  protected ArrayList<Object> _refs
    = new ArrayList<Object>();
  protected ArrayList<ObjectDefinition> _classDefs
    = new ArrayList<ObjectDefinition>();
  protected ArrayList<String> _types
    = new ArrayList<String>();
  
  // the underlying input stream
  private InputStream _is;
  private readonly byte[] _buffer = new byte[SIZE];
  
  // a peek character
  private int _offset;
  private int _length;

  // the method for a call
  private string _method;
  private Throwable _replyFault;

  private StringBuilder _sbuf = new StringBuilder();
  
  // true if this is the last chunk
  private bool _isLastChunk;
  // the chunk length
  private int _chunkLength;
  
  private HessianDebugInputStream _dIs;
  
  public Hessian2Input()
  {
    if (log.IsLoggable(Level.FINEST)) {
      _dIs = new HessianDebugInputStream(log, Level.FINEST);
    }
  }
  
  /// <summary>
  /// Creates a new Hessian input stream, initialized with an
  /// underlying input stream.
  ///
  /// <param name="is">the underlying input stream.</param>
  /// </summary>
  public Hessian2Input(InputStream is)
  {
    This();
    
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
    // the default serializer factory cannot be modified by external
    // callers
    if (_serializerFactory == _defaultSerializerFactory) {
      _serializerFactory = new SerializerFactory();
    }
    
    return _serializerFactory;
  }

  /// <summary>
  /// Gets the serializer factory.
  /// </summary>
  protected readonly SerializerFactory FindSerializerFactory()
  {
    SerializerFactory factory = _serializerFactory;

    if (factory == null) {
      factory = SerializerFactory.CreateDefault();
      _defaultSerializerFactory = factory;
      _serializerFactory = factory;
    }

    return factory;
  }

  public void SetCloseStreamOnClose(bool isClose)
  {
    _isCloseStreamOnClose = isClose;
  }

  public bool IsCloseStreamOnClose()
  {
    return _isCloseStreamOnClose;
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

    public override void Init(InputStream is)
  {
    if (_dIs != null) {
      _dIs.InitPacket(is);
      is = _dIs;
    }
    
    _is = is;
    
    Reset();
  }

  public void InitPacket(InputStream is)
  {
    if (_dIs != null) {
      _dIs.InitPacket(is);
      is = _dIs;
    }
    
    _is = is;
    
    ResetReferences();
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
    
    if (tag != 'C')
      throw Error("expected hessian call ('C') at " + CodeName(tag));

    return 0;
  }

  /// <summary>
  /// Starts reading the envelope
  ///
  /// <code>
  /// E major minor
  /// </code>
  /// </summary>
  public int ReadEnvelope()
      {
    int tag = Read();
    int version = 0;

    if (tag == 'H') {
      int major = Read();
      int minor = Read();

      version = (major << 16) + minor;

      tag = Read();
    }
    
    if (tag != 'E')
      throw Error("expected hessian Envelope ('E') at " + CodeName(tag));

    return version;
  }

  /// <summary>
  /// Completes reading the envelope
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// Z
  /// </code>
  /// </summary>
  public void CompleteEnvelope()
      {
    int tag = Read();
    
    if (tag != 'Z')
      Error("expected end of envelope at " + CodeName(tag));
  }

  /// <summary>
  /// Starts reading the call
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// string
  /// </code>
  /// </summary>
  public string ReadMethod()
      {
    _method = ReadString();

    return _method;
  }

  /// <summary>
  /// Returns the number of method arguments
  ///
  /// <code>
  /// int
  /// </code>
  /// </summary>
    public override int ReadMethodArgLength()
      {
    return ReadInt();
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

    ReadMethod();
  }

  public object[] ReadArguments()
      {
    int len = ReadInt();

    object[] args = new Object[len];

    for (int i = 0; i < len; i++)
      args[i] = ReadObject();

    return args;
  }

  /// <summary>
  /// Completes reading the call
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// </code>
  /// </summary>
  public void CompleteCall()
      {
  }

  /// <summary>
  /// Reads a reply as an object.
  /// If the reply has a fault, exception.
  /// </summary>
    public override object ReadReply(Class expectedClass)
  {
    int tag = Read();

    if (tag == 'R')
      return ReadObject(expectedClass);
    else if (tag == 'F') {
      HashMap map = (HashMap) ReadObject(HashMap.class);

      throw PrepareFault(map);
    }
    else {
      StringBuilder sb = new StringBuilder();
      sb.Append((char) tag);
      
      try {
        int ch;

        while ((ch = Read()) >= 0) {
          sb.Append((char) ch);
        }
      } catch (IOException e) {
        log.Log(Level.FINE, e.ToString(), e);
      }
      
      throw Error("expected hessian reply at " + CodeName(tag) + "\n"
                  + sb);
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
    // XXX: for variable length (?)
    
    ReadReply(Object.class);
  }

  /// <summary>
  /// Prepares the fault.
  /// </summary>
  private Throwable PrepareFault(HashMap fault)
      {
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
    
    if (tag != 'Z')
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
    return null;
  }

  /// <summary>
  /// Starts reading a packet
  ///
  /// <code>
  /// p major minor
  /// </code>
  /// </summary>
  public int StartMessage()
      {
    int tag = Read();

    if (tag == 'p') {
    } else if (tag == 'P') {
    } else
      throw Error("expected Hessian message ('p') at " + CodeName(tag));

    int major = Read();
    int minor = Read();

    return (major << 16) + minor;
  }

  /// <summary>
  /// Completes reading the message
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// z
  /// </code>
  /// </summary>
  public void CompleteMessage()
      {
    int tag = Read();
    
    if (tag != 'Z')
      Error("expected end of message at " + CodeName(tag));
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
    int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    switch (tag) {
    case 'T': return true;
    case 'F': return false;

      // direct integer
    case 0x80: case 0x81: case 0x82: case 0x83:
    case 0x84: case 0x85: case 0x86: case 0x87:
    case 0x88: case 0x89: case 0x8a: case 0x8b:
    case 0x8c: case 0x8d: case 0x8e: case 0x8f:
      
    case 0x90: case 0x91: case 0x92: case 0x93:
    case 0x94: case 0x95: case 0x96: case 0x97:
    case 0x98: case 0x99: case 0x9a: case 0x9b:
    case 0x9c: case 0x9d: case 0x9e: case 0x9f:
      
    case 0xa0: case 0xa1: case 0xa2: case 0xa3:
    case 0xa4: case 0xa5: case 0xa6: case 0xa7:
    case 0xa8: case 0xa9: case 0xaa: case 0xab:
    case 0xac: case 0xad: case 0xae: case 0xaf:

    case 0xb0: case 0xb1: case 0xb2: case 0xb3:
    case 0xb4: case 0xb5: case 0xb6: case 0xb7:
    case 0xb8: case 0xb9: case 0xba: case 0xbb:
    case 0xbc: case 0xbd: case 0xbe: case 0xbf:
      return tag != BC_INT_ZERO;

      // INT_BYTE = 0
    case 0xc8: 
      return Read() != 0;
      
      // INT_BYTE != 0
    case 0xc0: case 0xc1: case 0xc2: case 0xc3:
    case 0xc4: case 0xc5: case 0xc6: case 0xc7:
    case 0xc9: case 0xca: case 0xcb:
    case 0xcc: case 0xcd: case 0xce: case 0xcf:
      Read();
      return true;

      // INT_SHORT = 0
    case 0xd4: 
      return (256/// Read() + Read()) != 0;
      
      // INT_SHORT != 0
    case 0xd0: case 0xd1: case 0xd2: case 0xd3:
    case 0xd5: case 0xd6: case 0xd7:
      Read();
      Read();
      return true;
      
    case 'I': return
        ParseInt() != 0;
      
    case 0xd8: case 0xd9: case 0xda: case 0xdb:
    case 0xdc: case 0xdd: case 0xde: case 0xdf:
      
    case 0xe0: case 0xe1: case 0xe2: case 0xe3:
    case 0xe4: case 0xe5: case 0xe6: case 0xe7:
    case 0xe8: case 0xe9: case 0xea: case 0xeb:
    case 0xec: case 0xed: case 0xee: case 0xef:
      return tag != BC_LONG_ZERO;

      // LONG_BYTE = 0
    case 0xf8: 
      return Read() != 0;
      
      // LONG_BYTE != 0
    case 0xf0: case 0xf1: case 0xf2: case 0xf3:
    case 0xf4: case 0xf5: case 0xf6: case 0xf7:
    case 0xf9: case 0xfa: case 0xfb:
    case 0xfc: case 0xfd: case 0xfe: case 0xff:
      Read();
      return true;

      // INT_SHORT = 0
    case 0x3c: 
      return (256/// Read() + Read()) != 0;
      
      // INT_SHORT != 0
    case 0x38: case 0x39: case 0x3a: case 0x3b:
    case 0x3d: case 0x3e: case 0x3f:
      Read();
      Read();
      return true;

    case BC_LONG_INT:
      return (0x1000000L/// Read()
              + 0x10000L/// Read()
              + 0x100/// Read()
              + Read()) != 0;
      
    case 'L':
      return ParseLong() != 0;

    case BC_DOUBLE_ZERO:
      return false;
      
    case BC_DOUBLE_ONE:
      return true;
      
    case BC_DOUBLE_BYTE:
      return Read() != 0;
      
    case BC_DOUBLE_SHORT:
      return (0x100/// Read() + Read()) != 0;
      
    case BC_DOUBLE_MILL:
      {
        int mills = ParseInt();

        return mills != 0;
      }
      
    case 'D':
      return ParseDouble() != 0.0;
      
    case 'N':
      return false;
      
    default:
      throw Expect("bool", tag);
    }
  }

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
  public readonly int ReadInt()
      {
    //int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();
    int tag = Read();

    switch (tag) {
    case 'N':
      return 0;
      
    case 'F':
      return 0;
      
    case 'T':
      return 1;

      // direct integer
    case 0x80: case 0x81: case 0x82: case 0x83:
    case 0x84: case 0x85: case 0x86: case 0x87:
    case 0x88: case 0x89: case 0x8a: case 0x8b:
    case 0x8c: case 0x8d: case 0x8e: case 0x8f:
      
    case 0x90: case 0x91: case 0x92: case 0x93:
    case 0x94: case 0x95: case 0x96: case 0x97:
    case 0x98: case 0x99: case 0x9a: case 0x9b:
    case 0x9c: case 0x9d: case 0x9e: case 0x9f:
      
    case 0xa0: case 0xa1: case 0xa2: case 0xa3:
    case 0xa4: case 0xa5: case 0xa6: case 0xa7:
    case 0xa8: case 0xa9: case 0xaa: case 0xab:
    case 0xac: case 0xad: case 0xae: case 0xaf:
      
    case 0xb0: case 0xb1: case 0xb2: case 0xb3:
    case 0xb4: case 0xb5: case 0xb6: case 0xb7:
    case 0xb8: case 0xb9: case 0xba: case 0xbb:
    case 0xbc: case 0xbd: case 0xbe: case 0xbf:
      return tag - BC_INT_ZERO;

      /* byte int/// </summary>
    case 0xc0: case 0xc1: case 0xc2: case 0xc3:
    case 0xc4: case 0xc5: case 0xc6: case 0xc7:
    case 0xc8: case 0xc9: case 0xca: case 0xcb:
    case 0xcc: case 0xcd: case 0xce: case 0xcf:
      return ((tag - BC_INT_BYTE_ZERO) << 8) + Read();
      
      /* short int/// </summary>
    case 0xd0: case 0xd1: case 0xd2: case 0xd3:
    case 0xd4: case 0xd5: case 0xd6: case 0xd7:
      return ((tag - BC_INT_SHORT_ZERO) << 16) + 256/// Read() + Read();

    case 'I':
    case BC_LONG_INT:
      return ((Read() << 24)
              + (Read() << 16)
              + (Read() << 8)
              + Read());

      // direct long
    case 0xd8: case 0xd9: case 0xda: case 0xdb:
    case 0xdc: case 0xdd: case 0xde: case 0xdf:
      
    case 0xe0: case 0xe1: case 0xe2: case 0xe3:
    case 0xe4: case 0xe5: case 0xe6: case 0xe7:
    case 0xe8: case 0xe9: case 0xea: case 0xeb:
    case 0xec: case 0xed: case 0xee: case 0xef:
      return tag - BC_LONG_ZERO;

      /* byte long/// </summary>
    case 0xf0: case 0xf1: case 0xf2: case 0xf3:
    case 0xf4: case 0xf5: case 0xf6: case 0xf7:
    case 0xf8: case 0xf9: case 0xfa: case 0xfb:
    case 0xfc: case 0xfd: case 0xfe: case 0xff:
      return ((tag - BC_LONG_BYTE_ZERO) << 8) + Read();
      
      /* short long/// </summary>
    case 0x38: case 0x39: case 0x3a: case 0x3b:
    case 0x3c: case 0x3d: case 0x3e: case 0x3f:
      return ((tag - BC_LONG_SHORT_ZERO) << 16) + 256/// Read() + Read();

    case 'L':
      return (int) ParseLong();

    case BC_DOUBLE_ZERO:
      return 0;

    case BC_DOUBLE_ONE:
      return 1;

      //case LONG_BYTE:
    case BC_DOUBLE_BYTE:
      return (byte) (_offset < _length ? _buffer[_offset++] : Read());

      //case INT_SHORT:
      //case LONG_SHORT:
    case BC_DOUBLE_SHORT:
      return (short) (256/// Read() + Read());

    case BC_DOUBLE_MILL:
      {
        int mills = ParseInt();

        return (int) (0.001/// mills);
      }

    case 'D':
      return (int) ParseDouble();
      
    default:
      throw Expect("integer", tag);
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
    case 'N':
      return 0;
      
    case 'F':
      return 0;
      
    case 'T':
      return 1;

      // direct integer
    case 0x80: case 0x81: case 0x82: case 0x83:
    case 0x84: case 0x85: case 0x86: case 0x87:
    case 0x88: case 0x89: case 0x8a: case 0x8b:
    case 0x8c: case 0x8d: case 0x8e: case 0x8f:
      
    case 0x90: case 0x91: case 0x92: case 0x93:
    case 0x94: case 0x95: case 0x96: case 0x97:
    case 0x98: case 0x99: case 0x9a: case 0x9b:
    case 0x9c: case 0x9d: case 0x9e: case 0x9f:
      
    case 0xa0: case 0xa1: case 0xa2: case 0xa3:
    case 0xa4: case 0xa5: case 0xa6: case 0xa7:
    case 0xa8: case 0xa9: case 0xaa: case 0xab:
    case 0xac: case 0xad: case 0xae: case 0xaf:
      
    case 0xb0: case 0xb1: case 0xb2: case 0xb3:
    case 0xb4: case 0xb5: case 0xb6: case 0xb7:
    case 0xb8: case 0xb9: case 0xba: case 0xbb:
    case 0xbc: case 0xbd: case 0xbe: case 0xbf:
      return tag - BC_INT_ZERO;

      /* byte int/// </summary>
    case 0xc0: case 0xc1: case 0xc2: case 0xc3:
    case 0xc4: case 0xc5: case 0xc6: case 0xc7:
    case 0xc8: case 0xc9: case 0xca: case 0xcb:
    case 0xcc: case 0xcd: case 0xce: case 0xcf:
      return ((tag - BC_INT_BYTE_ZERO) << 8) + Read();
      
      /* short int/// </summary>
    case 0xd0: case 0xd1: case 0xd2: case 0xd3:
    case 0xd4: case 0xd5: case 0xd6: case 0xd7:
      return ((tag - BC_INT_SHORT_ZERO) << 16) + 256/// Read() + Read();

      //case LONG_BYTE:
    case BC_DOUBLE_BYTE:
      return (byte) (_offset < _length ? _buffer[_offset++] : Read());

      //case INT_SHORT:
      //case LONG_SHORT:
    case BC_DOUBLE_SHORT:
      return (short) (256/// Read() + Read());

    case 'I':
    case BC_LONG_INT:
      return ParseInt();

      // direct long
    case 0xd8: case 0xd9: case 0xda: case 0xdb:
    case 0xdc: case 0xdd: case 0xde: case 0xdf:
      
    case 0xe0: case 0xe1: case 0xe2: case 0xe3:
    case 0xe4: case 0xe5: case 0xe6: case 0xe7:
    case 0xe8: case 0xe9: case 0xea: case 0xeb:
    case 0xec: case 0xed: case 0xee: case 0xef:
      return tag - BC_LONG_ZERO;

      /* byte long/// </summary>
    case 0xf0: case 0xf1: case 0xf2: case 0xf3:
    case 0xf4: case 0xf5: case 0xf6: case 0xf7:
    case 0xf8: case 0xf9: case 0xfa: case 0xfb:
    case 0xfc: case 0xfd: case 0xfe: case 0xff:
      return ((tag - BC_LONG_BYTE_ZERO) << 8) + Read();
      
      /* short long/// </summary>
    case 0x38: case 0x39: case 0x3a: case 0x3b:
    case 0x3c: case 0x3d: case 0x3e: case 0x3f:
      return ((tag - BC_LONG_SHORT_ZERO) << 16) + 256/// Read() + Read();

    case 'L':
      return ParseLong();

    case BC_DOUBLE_ZERO:
      return 0;

    case BC_DOUBLE_ONE:
      return 1;

    case BC_DOUBLE_MILL:
      {
        int mills = ParseInt();

        return (long) (0.001/// mills);
      }

    case 'D':
      return (long) ParseDouble();
      
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
    case 'N':
      return 0;
      
    case 'F':
      return 0;
      
    case 'T':
      return 1;

      // direct integer
    case 0x80: case 0x81: case 0x82: case 0x83:
    case 0x84: case 0x85: case 0x86: case 0x87:
    case 0x88: case 0x89: case 0x8a: case 0x8b:
    case 0x8c: case 0x8d: case 0x8e: case 0x8f:
      
    case 0x90: case 0x91: case 0x92: case 0x93:
    case 0x94: case 0x95: case 0x96: case 0x97:
    case 0x98: case 0x99: case 0x9a: case 0x9b:
    case 0x9c: case 0x9d: case 0x9e: case 0x9f:
      
    case 0xa0: case 0xa1: case 0xa2: case 0xa3:
    case 0xa4: case 0xa5: case 0xa6: case 0xa7:
    case 0xa8: case 0xa9: case 0xaa: case 0xab:
    case 0xac: case 0xad: case 0xae: case 0xaf:
      
    case 0xb0: case 0xb1: case 0xb2: case 0xb3:
    case 0xb4: case 0xb5: case 0xb6: case 0xb7:
    case 0xb8: case 0xb9: case 0xba: case 0xbb:
    case 0xbc: case 0xbd: case 0xbe: case 0xbf:
      return tag - 0x90;

      /* byte int/// </summary>
    case 0xc0: case 0xc1: case 0xc2: case 0xc3:
    case 0xc4: case 0xc5: case 0xc6: case 0xc7:
    case 0xc8: case 0xc9: case 0xca: case 0xcb:
    case 0xcc: case 0xcd: case 0xce: case 0xcf:
      return ((tag - BC_INT_BYTE_ZERO) << 8) + Read();
      
      /* short int/// </summary>
    case 0xd0: case 0xd1: case 0xd2: case 0xd3:
    case 0xd4: case 0xd5: case 0xd6: case 0xd7:
      return ((tag - BC_INT_SHORT_ZERO) << 16) + 256/// Read() + Read();

    case 'I':
    case BC_LONG_INT:
      return ParseInt();

      // direct long
    case 0xd8: case 0xd9: case 0xda: case 0xdb:
    case 0xdc: case 0xdd: case 0xde: case 0xdf:
      
    case 0xe0: case 0xe1: case 0xe2: case 0xe3:
    case 0xe4: case 0xe5: case 0xe6: case 0xe7:
    case 0xe8: case 0xe9: case 0xea: case 0xeb:
    case 0xec: case 0xed: case 0xee: case 0xef:
      return tag - BC_LONG_ZERO;

      /* byte long/// </summary>
    case 0xf0: case 0xf1: case 0xf2: case 0xf3:
    case 0xf4: case 0xf5: case 0xf6: case 0xf7:
    case 0xf8: case 0xf9: case 0xfa: case 0xfb:
    case 0xfc: case 0xfd: case 0xfe: case 0xff:
      return ((tag - BC_LONG_BYTE_ZERO) << 8) + Read();
      
      /* short long/// </summary>
    case 0x38: case 0x39: case 0x3a: case 0x3b:
    case 0x3c: case 0x3d: case 0x3e: case 0x3f:
      return ((tag - BC_LONG_SHORT_ZERO) << 16) + 256/// Read() + Read();

    case 'L':
      return (double) ParseLong();

    case BC_DOUBLE_ZERO:
      return 0;

    case BC_DOUBLE_ONE:
      return 1;

    case BC_DOUBLE_BYTE:
      return (byte) (_offset < _length ? _buffer[_offset++] : Read());

    case BC_DOUBLE_SHORT:
      return (short) (256/// Read() + Read());

    case BC_DOUBLE_MILL:
      {
        int mills = ParseInt();

        return 0.001/// mills;
      }
      
    case 'D':
      return ParseDouble();
      
    default:
      throw Expect("double", tag);
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

    if (tag == BC_DATE) {
      return ParseLong();
    }
    else if (tag == BC_DATE_MINUTE) {
      return ParseInt()/// 60000L;
    }
    else
      throw Expect("date", tag);
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
    case BC_STRING_CHUNK:
      _isLastChunk = tag == 'S';
      _chunkLength = (Read() << 8) + Read();

      _chunkLength--;
      int value = ParseUTF8Char();

      // special code so successive read byte won't
      // be read as a single object.
      if (_chunkLength == 0 && _isLastChunk)
        _chunkLength = END_OF_DATA;

      return value;
      
    default:
      throw Expect("char", tag);
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
      case BC_STRING_CHUNK:
        _isLastChunk = tag == 'S';
        _chunkLength = (Read() << 8) + Read();
        break;

      case 0x00: case 0x01: case 0x02: case 0x03:
      case 0x04: case 0x05: case 0x06: case 0x07:
      case 0x08: case 0x09: case 0x0a: case 0x0b:
      case 0x0c: case 0x0d: case 0x0e: case 0x0f:

      case 0x10: case 0x11: case 0x12: case 0x13:
      case 0x14: case 0x15: case 0x16: case 0x17:
      case 0x18: case 0x19: case 0x1a: case 0x1b:
      case 0x1c: case 0x1d: case 0x1e: case 0x1f:
        _isLastChunk = true;
        _chunkLength = tag - 0x00;
        break;

      case 0x30: case 0x31: case 0x32: case 0x33:
        _isLastChunk = true;
        _chunkLength = (tag - 0x30)/// 256 + Read();
        break;

      default:
        throw Expect("string", tag);
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
        case BC_STRING_CHUNK:
          _isLastChunk = tag == 'S';
          _chunkLength = (Read() << 8) + Read();
          break;

        case 0x00: case 0x01: case 0x02: case 0x03:
        case 0x04: case 0x05: case 0x06: case 0x07:
        case 0x08: case 0x09: case 0x0a: case 0x0b:
        case 0x0c: case 0x0d: case 0x0e: case 0x0f:

        case 0x10: case 0x11: case 0x12: case 0x13:
        case 0x14: case 0x15: case 0x16: case 0x17:
        case 0x18: case 0x19: case 0x1a: case 0x1b:
        case 0x1c: case 0x1d: case 0x1e: case 0x1f:
          _isLastChunk = true;
          _chunkLength = tag - 0x00;
          break;

        case 0x30: case 0x31: case 0x32: case 0x33:
          _isLastChunk = true;
          _chunkLength = (tag - 0x30)/// 256 + Read();
          break;
      
        default:
          throw Expect("string", tag);
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
    case 'T':
      return "true";
    case 'F':
      return "false";

      // direct integer
    case 0x80: case 0x81: case 0x82: case 0x83:
    case 0x84: case 0x85: case 0x86: case 0x87:
    case 0x88: case 0x89: case 0x8a: case 0x8b:
    case 0x8c: case 0x8d: case 0x8e: case 0x8f:
      
    case 0x90: case 0x91: case 0x92: case 0x93:
    case 0x94: case 0x95: case 0x96: case 0x97:
    case 0x98: case 0x99: case 0x9a: case 0x9b:
    case 0x9c: case 0x9d: case 0x9e: case 0x9f:
      
    case 0xa0: case 0xa1: case 0xa2: case 0xa3:
    case 0xa4: case 0xa5: case 0xa6: case 0xa7:
    case 0xa8: case 0xa9: case 0xaa: case 0xab:
    case 0xac: case 0xad: case 0xae: case 0xaf:
      
    case 0xb0: case 0xb1: case 0xb2: case 0xb3:
    case 0xb4: case 0xb5: case 0xb6: case 0xb7:
    case 0xb8: case 0xb9: case 0xba: case 0xbb:
    case 0xbc: case 0xbd: case 0xbe: case 0xbf:
      return String.ValueOf((tag - 0x90));

      /* byte int/// </summary>
    case 0xc0: case 0xc1: case 0xc2: case 0xc3:
    case 0xc4: case 0xc5: case 0xc6: case 0xc7:
    case 0xc8: case 0xc9: case 0xca: case 0xcb:
    case 0xcc: case 0xcd: case 0xce: case 0xcf:
      return String.ValueOf(((tag - BC_INT_BYTE_ZERO) << 8) + Read());
      
      /* short int/// </summary>
    case 0xd0: case 0xd1: case 0xd2: case 0xd3:
    case 0xd4: case 0xd5: case 0xd6: case 0xd7:
      return String.ValueOf(((tag - BC_INT_SHORT_ZERO) << 16)
                            + 256/// Read() + Read());

    case 'I':
    case BC_LONG_INT:
      return String.ValueOf(ParseInt());

      // direct long
    case 0xd8: case 0xd9: case 0xda: case 0xdb:
    case 0xdc: case 0xdd: case 0xde: case 0xdf:
      
    case 0xe0: case 0xe1: case 0xe2: case 0xe3:
    case 0xe4: case 0xe5: case 0xe6: case 0xe7:
    case 0xe8: case 0xe9: case 0xea: case 0xeb:
    case 0xec: case 0xed: case 0xee: case 0xef:
      return String.ValueOf(tag - BC_LONG_ZERO);

      /* byte long/// </summary>
    case 0xf0: case 0xf1: case 0xf2: case 0xf3:
    case 0xf4: case 0xf5: case 0xf6: case 0xf7:
    case 0xf8: case 0xf9: case 0xfa: case 0xfb:
    case 0xfc: case 0xfd: case 0xfe: case 0xff:
      return String.ValueOf(((tag - BC_LONG_BYTE_ZERO) << 8) + Read());
      
      /* short long/// </summary>
    case 0x38: case 0x39: case 0x3a: case 0x3b:
    case 0x3c: case 0x3d: case 0x3e: case 0x3f:
      return String.ValueOf(((tag - BC_LONG_SHORT_ZERO) << 16)
                            + 256/// Read() + Read());

    case 'L':
      return String.ValueOf(ParseLong());

    case BC_DOUBLE_ZERO:
      return "0.0";

    case BC_DOUBLE_ONE:
      return "1.0";

    case BC_DOUBLE_BYTE:
      return String.ValueOf((byte) (_offset < _length
                                    ? _buffer[_offset++]
                                    : Read()));

    case BC_DOUBLE_SHORT:
      return String.ValueOf(((short) (256/// Read() + Read())));

    case BC_DOUBLE_MILL:
      {
        int mills = ParseInt();

        return String.ValueOf(0.001/// mills);
      }
      
    case 'D':
      return String.ValueOf(ParseDouble());

    case 'S':
    case BC_STRING_CHUNK:
      _isLastChunk = tag == 'S';
      _chunkLength = (Read() << 8) + Read();

      _sbuf.SetLength(0);
      int ch;

      while ((ch = ParseChar()) >= 0)
        _sbuf.Append((char) ch);

      return _sbuf.ToString();

      // 0-byte string
    case 0x00: case 0x01: case 0x02: case 0x03:
    case 0x04: case 0x05: case 0x06: case 0x07:
    case 0x08: case 0x09: case 0x0a: case 0x0b:
    case 0x0c: case 0x0d: case 0x0e: case 0x0f:

    case 0x10: case 0x11: case 0x12: case 0x13:
    case 0x14: case 0x15: case 0x16: case 0x17:
    case 0x18: case 0x19: case 0x1a: case 0x1b:
    case 0x1c: case 0x1d: case 0x1e: case 0x1f:
      _isLastChunk = true;
      _chunkLength = tag - 0x00;

      _sbuf.SetLength(0);

      while ((ch = ParseChar()) >= 0) {
        _sbuf.Append((char) ch);
      }

      return _sbuf.ToString();

    case 0x30: case 0x31: case 0x32: case 0x33:
      _isLastChunk = true;
      _chunkLength = (tag - 0x30)/// 256 + Read();

      _sbuf.SetLength(0);

      while ((ch = ParseChar()) >= 0)
        _sbuf.Append((char) ch);

      return _sbuf.ToString();

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

    case BC_BINARY:
    case BC_BINARY_CHUNK:
      _isLastChunk = tag == BC_BINARY;
      _chunkLength = (Read() << 8) + Read();

      ByteArrayOutputStream bos = new ByteArrayOutputStream();

      int data;
      while ((data = ParseByte()) >= 0)
        bos.Write(data);

      return bos.ToByteArray();

    case 0x20: case 0x21: case 0x22: case 0x23:
    case 0x24: case 0x25: case 0x26: case 0x27:
    case 0x28: case 0x29: case 0x2a: case 0x2b:
    case 0x2c: case 0x2d: case 0x2e: case 0x2f:
      {
        _isLastChunk = true;
        _chunkLength = tag - 0x20;

        byte[] buffer = new byte[_chunkLength];

        int offset = 0;
        while (offset < _chunkLength) {
          int sublen = Read(buffer, 0, _chunkLength - offset);

          if (sublen <= 0)
            break;

          offset += sublen;
        }

        return buffer;
      }
      
    case 0x34: case 0x35: case 0x36: case 0x37:
      {
        _isLastChunk = true;
        _chunkLength = (tag - 0x34)/// 256 + Read();

        byte[] buffer = new byte[_chunkLength];

        int offset = 0;
        while (offset < _chunkLength) {
          int sublen = Read(buffer, 0, _chunkLength - offset);

          if (sublen <= 0)
            break;

          offset += sublen;
        }

        return buffer;
      }
      
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
    case BC_BINARY_CHUNK:
      {
        _isLastChunk = tag == 'B';
        _chunkLength = (Read() << 8) + Read();

        int value = ParseByte();

        // special code so successive read byte won't
        // be read as a single object.
        if (_chunkLength == 0 && _isLastChunk)
          _chunkLength = END_OF_DATA;

        return value;
      }

    case 0x20: case 0x21: case 0x22: case 0x23:
    case 0x24: case 0x25: case 0x26: case 0x27:
    case 0x28: case 0x29: case 0x2a: case 0x2b:
    case 0x2c: case 0x2d: case 0x2e: case 0x2f:
      {
        _isLastChunk = true;
        _chunkLength = tag - 0x20;

        int value = ParseByte();

        // special code so successive read byte won't
        // be read as a single object.
        if (_chunkLength == 0)
          _chunkLength = END_OF_DATA;

        return value;
      }
      
    case 0x34: case 0x35: case 0x36: case 0x37:
      {
        _isLastChunk = true;
        _chunkLength = (tag - 0x34)/// 256 + Read();

        int value = ParseByte();

        // special code so successive read byte won't
        // be read as a single object.
        if (_chunkLength == 0)
          _chunkLength = END_OF_DATA;

        return value;
      }
      
    default:
      throw Expect("binary", tag);
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
      case BC_BINARY_CHUNK:
        _isLastChunk = tag == 'B';
        _chunkLength = (Read() << 8) + Read();
        break;

    case 0x20: case 0x21: case 0x22: case 0x23:
    case 0x24: case 0x25: case 0x26: case 0x27:
    case 0x28: case 0x29: case 0x2a: case 0x2b:
    case 0x2c: case 0x2d: case 0x2e: case 0x2f:
      {
        _isLastChunk = true;
        _chunkLength = tag - 0x20;
        break;
      }
      
    case 0x34: case 0x35: case 0x36: case 0x37:
      {
        _isLastChunk = true;
        _chunkLength = (tag - 0x34)/// 256 + Read();
        break;
      }
      
      default:
        throw Expect("binary", tag);
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
        case BC_BINARY_CHUNK:
          _isLastChunk = tag == 'B';
          _chunkLength = (Read() << 8) + Read();
          break;
      
        default:
          throw Expect("binary", tag);
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
    for (; code > 0 && code != 'Z'; code = Read()) {
      _offset--;
      
      object key = ReadObject();
      object value = ReadObject();

      if (key != null && value != null)
        map.Put(key, value);
    }

    if (code != 'Z')
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
    
    int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    switch (tag) {
    case 'N':
      return null;

    case 'H':
      {
        Deserializer reader = FindSerializerFactory().GetDeserializer(cl);

        return reader.ReadMap(this);
      }

    case 'M':
      {
        string type = ReadType();

        // hessian/3bb3
        if ("".Equals(type)) {
          Deserializer reader;
          reader = FindSerializerFactory().GetDeserializer(cl);

          return reader.ReadMap(this);
        }
        else {
          Deserializer reader;
          reader = FindSerializerFactory().GetObjectDeserializer(type, cl);

          return reader.ReadMap(this);
        }
      }

    case 'C':
      {
        ReadObjectDefinition(cl);

        return ReadObject(cl);
      }

    case 0x60: case 0x61: case 0x62: case 0x63:
    case 0x64: case 0x65: case 0x66: case 0x67:
    case 0x68: case 0x69: case 0x6a: case 0x6b:
    case 0x6c: case 0x6d: case 0x6e: case 0x6f:
      {
        int ref = tag - 0x60;
        int size = _classDefs.Size();

        if (ref < 0 || size <= ref)
          throw new HessianProtocolException("'" + ref + "' is an unknown class definition");

        ObjectDefinition def = _classDefs.Get(ref);

        return ReadObjectInstance(cl, def);
      }

    case 'O':
      {
        int ref = ReadInt();
        int size = _classDefs.Size();

        if (ref < 0 || size <= ref)
          throw new HessianProtocolException("'" + ref + "' is an unknown class definition");

        ObjectDefinition def = _classDefs.Get(ref);

        return ReadObjectInstance(cl, def);
      }

    case BC_LIST_VARIABLE:
      {
        string type = ReadType();
      
        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(type, cl);

        object v = reader.ReadList(this, -1);

        return v;
      }

    case BC_LIST_FIXED:
      {
        string type = ReadType();
        int length = ReadInt();
      
        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(type, cl);

        object v = reader.ReadLengthList(this, length);

        return v;
      }

    case 0x70: case 0x71: case 0x72: case 0x73:
    case 0x74: case 0x75: case 0x76: case 0x77:
      {
        int length = tag - 0x70;

        string type = ReadType();
      
        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(type, cl);

        object v = reader.ReadLengthList(this, length);

        return v;
      }

    case BC_LIST_VARIABLE_UNTYPED:
      {
        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(null, cl);

        object v = reader.ReadList(this, -1);

        return v;
      }

    case BC_LIST_FIXED_UNTYPED:
      {
        int length = ReadInt();
      
        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(null, cl);

        object v = reader.ReadLengthList(this, length);

        return v;
      }

    case 0x78: case 0x79: case 0x7a: case 0x7b:
    case 0x7c: case 0x7d: case 0x7e: case 0x7f:
      {
        int length = tag - 0x78;
      
        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(null, cl);

        object v = reader.ReadLengthList(this, length);

        return v;
      }

    case BC_REF:
      {
        int ref = ReadInt();

        return _refs.Get(ref);
      }
    }

    if (tag >= 0)
      _offset--;

    // hessian/3b2i vs hessian/3406
    // return ReadObject();
    object value = FindSerializerFactory().GetDeserializer(cl).ReadObject(this);
    return value;
  }
  
  /// <summary>
  /// Reads an arbitrary object from the input stream when the type
  /// is unknown.
  /// </summary>
  public object ReadObject()
      {
    int tag = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    switch (tag) {
    case 'N':
      return null;
      
    case 'T':
      return Boolean.ValueOf(true);
      
    case 'F':
      return Boolean.ValueOf(false);

      // direct integer
    case 0x80: case 0x81: case 0x82: case 0x83:
    case 0x84: case 0x85: case 0x86: case 0x87:
    case 0x88: case 0x89: case 0x8a: case 0x8b:
    case 0x8c: case 0x8d: case 0x8e: case 0x8f:
      
    case 0x90: case 0x91: case 0x92: case 0x93:
    case 0x94: case 0x95: case 0x96: case 0x97:
    case 0x98: case 0x99: case 0x9a: case 0x9b:
    case 0x9c: case 0x9d: case 0x9e: case 0x9f:
      
    case 0xa0: case 0xa1: case 0xa2: case 0xa3:
    case 0xa4: case 0xa5: case 0xa6: case 0xa7:
    case 0xa8: case 0xa9: case 0xaa: case 0xab:
    case 0xac: case 0xad: case 0xae: case 0xaf:
      
    case 0xb0: case 0xb1: case 0xb2: case 0xb3:
    case 0xb4: case 0xb5: case 0xb6: case 0xb7:
    case 0xb8: case 0xb9: case 0xba: case 0xbb:
    case 0xbc: case 0xbd: case 0xbe: case 0xbf:
      return Integer.ValueOf(tag - BC_INT_ZERO);

      /* byte int/// </summary>
    case 0xc0: case 0xc1: case 0xc2: case 0xc3:
    case 0xc4: case 0xc5: case 0xc6: case 0xc7:
    case 0xc8: case 0xc9: case 0xca: case 0xcb:
    case 0xcc: case 0xcd: case 0xce: case 0xcf:
      return Integer.ValueOf(((tag - BC_INT_BYTE_ZERO) << 8) + Read());
      
      /* short int/// </summary>
    case 0xd0: case 0xd1: case 0xd2: case 0xd3:
    case 0xd4: case 0xd5: case 0xd6: case 0xd7:
      return Integer.ValueOf(((tag - BC_INT_SHORT_ZERO) << 16)
                             + 256/// Read() + Read());
      
    case 'I':
      return Integer.ValueOf(ParseInt());

      // direct long
    case 0xd8: case 0xd9: case 0xda: case 0xdb:
    case 0xdc: case 0xdd: case 0xde: case 0xdf:
      
    case 0xe0: case 0xe1: case 0xe2: case 0xe3:
    case 0xe4: case 0xe5: case 0xe6: case 0xe7:
    case 0xe8: case 0xe9: case 0xea: case 0xeb:
    case 0xec: case 0xed: case 0xee: case 0xef:
      return Long.ValueOf(tag - BC_LONG_ZERO);

      /* byte long/// </summary>
    case 0xf0: case 0xf1: case 0xf2: case 0xf3:
    case 0xf4: case 0xf5: case 0xf6: case 0xf7:
    case 0xf8: case 0xf9: case 0xfa: case 0xfb:
    case 0xfc: case 0xfd: case 0xfe: case 0xff:
      return Long.ValueOf(((tag - BC_LONG_BYTE_ZERO) << 8) + Read());
      
      /* short long/// </summary>
    case 0x38: case 0x39: case 0x3a: case 0x3b:
    case 0x3c: case 0x3d: case 0x3e: case 0x3f:
      return Long.ValueOf(((tag - BC_LONG_SHORT_ZERO) << 16) + 256/// Read() + Read());
      
    case BC_LONG_INT:
      return Long.ValueOf(ParseInt());
    
    case 'L':
      return Long.ValueOf(ParseLong());

    case BC_DOUBLE_ZERO:
      return Double.ValueOf(0);

    case BC_DOUBLE_ONE:
      return Double.ValueOf(1);

    case BC_DOUBLE_BYTE:
      return Double.ValueOf((byte) Read());

    case BC_DOUBLE_SHORT:
      return Double.ValueOf((short) (256/// Read() + Read()));
      
    case BC_DOUBLE_MILL:
      {
        int mills = ParseInt();

        return Double.ValueOf(0.001/// mills);
      }

    case 'D':
      return Double.ValueOf(ParseDouble());
    
    case BC_DATE:
      return new Date(ParseLong());
    
    case BC_DATE_MINUTE:
      return new Date(ParseInt()/// 60000L);

    case BC_STRING_CHUNK:
    case 'S':
      {
        _isLastChunk = tag == 'S';
        _chunkLength = (Read() << 8) + Read();

        _sbuf.SetLength(0);
        
        ParseString(_sbuf);

        return _sbuf.ToString();
      }

    case 0x00: case 0x01: case 0x02: case 0x03:
    case 0x04: case 0x05: case 0x06: case 0x07:
    case 0x08: case 0x09: case 0x0a: case 0x0b:
    case 0x0c: case 0x0d: case 0x0e: case 0x0f:

    case 0x10: case 0x11: case 0x12: case 0x13:
    case 0x14: case 0x15: case 0x16: case 0x17:
    case 0x18: case 0x19: case 0x1a: case 0x1b:
    case 0x1c: case 0x1d: case 0x1e: case 0x1f:
      {
        _isLastChunk = true;
        _chunkLength = tag - 0x00;

        int data;
        _sbuf.SetLength(0);
      
        ParseString(_sbuf);

        return _sbuf.ToString();
      }

    case 0x30: case 0x31: case 0x32: case 0x33:
      {
        _isLastChunk = true;
        _chunkLength = (tag - 0x30)/// 256 + Read();

        _sbuf.SetLength(0);

        ParseString(_sbuf);

        return _sbuf.ToString();
      }

    case BC_BINARY_CHUNK:
    case 'B':
      {
        _isLastChunk = tag == 'B';
        _chunkLength = (Read() << 8) + Read();

        int data;
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
      
        while ((data = ParseByte()) >= 0)
          bos.Write(data);

        return bos.ToByteArray();
      }

    case 0x20: case 0x21: case 0x22: case 0x23:
    case 0x24: case 0x25: case 0x26: case 0x27:
    case 0x28: case 0x29: case 0x2a: case 0x2b:
    case 0x2c: case 0x2d: case 0x2e: case 0x2f:
      {
        _isLastChunk = true;
        int len = tag - 0x20;
        _chunkLength = 0;

        byte[] data = new byte[len];

        for (int i = 0; i < len; i++)
          data[i] = (byte) Read();

        return data;
      }
      
    case 0x34: case 0x35: case 0x36: case 0x37:
      {
        _isLastChunk = true;
        int len = (tag - 0x34)/// 256 + Read();
        _chunkLength = 0;

        byte[] buffer = new byte[len];

        for (int i = 0; i < len; i++) {
          buffer[i] = (byte) Read();
        }

        return buffer;
      }

    case BC_LIST_VARIABLE:
      {
        // variable length list
        string type = ReadType();

        return FindSerializerFactory().ReadList(this, -1, type);
      }

    case BC_LIST_VARIABLE_UNTYPED:
      {
        return FindSerializerFactory().ReadList(this, -1, null);
      }

    case BC_LIST_FIXED:
      {
        // fixed length lists
        string type = ReadType();
        int length = ReadInt();

        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(type, null);
      
        return reader.ReadLengthList(this, length);
      }

    case BC_LIST_FIXED_UNTYPED:
      {
        // fixed length lists
        int length = ReadInt();

        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(null, null);
      
        return reader.ReadLengthList(this, length);
      }

      // compact fixed list
    case 0x70: case 0x71: case 0x72: case 0x73:
    case 0x74: case 0x75: case 0x76: case 0x77:
      {
        // fixed length lists
        string type = ReadType();
        int length = tag - 0x70;

        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(type, null);
      
        return reader.ReadLengthList(this, length);
      }

      // compact fixed untyped list
    case 0x78: case 0x79: case 0x7a: case 0x7b:
    case 0x7c: case 0x7d: case 0x7e: case 0x7f:
      {
        // fixed length lists
        int length = tag - 0x78;

        Deserializer reader;
        reader = FindSerializerFactory().GetListDeserializer(null, null);
      
        return reader.ReadLengthList(this, length);
      }

    case 'H':
      {
        return FindSerializerFactory().ReadMap(this, null);
      }

    case 'M':
      {
        string type = ReadType();

        return FindSerializerFactory().ReadMap(this, type);
      }

    case 'C':
      {
        ReadObjectDefinition(null);

        return ReadObject();
      }

    case 0x60: case 0x61: case 0x62: case 0x63:
    case 0x64: case 0x65: case 0x66: case 0x67:
    case 0x68: case 0x69: case 0x6a: case 0x6b:
    case 0x6c: case 0x6d: case 0x6e: case 0x6f:
      {
        int ref = tag - 0x60;

        if (_classDefs.Size() <= ref)
          throw Error("No classes defined at reference '"
                      + Integer.ToHexString(tag) + "'");

        ObjectDefinition def = _classDefs.Get(ref);

        return ReadObjectInstance(null, def);
      }

    case 'O':
      {
        int ref = ReadInt();

        if (_classDefs.Size() <= ref)
          throw Error("Illegal object reference #" + ref);

        ObjectDefinition def = _classDefs.Get(ref);

        return ReadObjectInstance(null, def);
      }

    case BC_REF:
      {
        int ref = ReadInt();

        return _refs.Get(ref);
      }

    default:
      if (tag < 0)
        throw new EOFException("readObject: unexpected end of file");
      else
        throw Error("readObject: unknown code " + CodeName(tag));
    }
  }

  /// <summary>
  /// Reads an object definition:
  ///
  /// <code>
  /// O string <int> (string)* <value>*
  /// </code>
  /// </summary>
  private void ReadObjectDefinition(Type cl)
      {
    string type = ReadString();
    int len = ReadInt();

    SerializerFactory factory = FindSerializerFactory();
    
    Deserializer reader = factory.GetObjectDeserializer(type, null);
    
    object[] fields = reader.CreateFields(len);
    string[] fieldNames = new string[len];
    
    for (int i = 0; i < len; i++) {
      string name = ReadString();
      
      fields[i] = reader.CreateField(name);
      fieldNames[i] = name;
    }
    
    ObjectDefinition def
      = new ObjectDefinition(type, reader, fields, fieldNames);

    _classDefs.Add(def);
  }

  private object ReadObjectInstance(Type cl,
                                    ObjectDefinition def)
      {
    string type = def.GetType();
    Deserializer reader = def.GetReader();
    object[] fields = def.GetFields();

    SerializerFactory factory = FindSerializerFactory();
    
    if (cl != reader.GetType() && cl != null) {
      reader = factory.GetObjectDeserializer(type, cl);
      
      return reader.ReadObject(this, def.GetFieldNames());
    }
    else {
      return reader.ReadObject(this, fields);
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
    int value = ParseInt();
    
    return _refs.Get(value);
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
    int code;

    if (_offset < _length)
      code = (_buffer[_offset] & 0xff);
    else {
      code = Read();

      if (code >= 0)
        _offset--;
    }

    return (code < 0 || code == 'Z');
  }

  /// <summary>
  /// Reads the end byte.
  /// </summary>
  public void ReadEnd()
      {
    int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    if (code == 'Z')
      return;
    else if (code < 0)
      throw Error("unexpected end of file");
    else
      throw Error("unknown code:" + CodeName(code));
  }

  /// <summary>
  /// Reads the end byte.
  /// </summary>
  public void ReadMapEnd()
      {
    int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    if (code != 'Z')
      throw Error("expected end of map ('Z') at '" + CodeName(code) + "'");
  }

  /// <summary>
  /// Reads the end byte.
  /// </summary>
  public void ReadListEnd()
      {
    int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    if (code != 'Z')
      throw Error("expected end of list ('Z') at '" + CodeName(code) + "'");
  }

  /// <summary>
  /// Adds a list/map reference.
  /// </summary>
    public override int AddRef(object ref)
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
    public override void ResetReferences()
  {
    _refs.Clear();
  }
  
  public void Reset()
  {
    ResetReferences();
    
    _classDefs.Clear();
    _types.Clear();
  }

  public void ResetBuffer()
  {
    int offset = _offset;
    _offset = 0;
    
    int length = _length;
    _length = 0;

    if (length > 0 && offset != length)
      throw new IllegalStateException("offset=" + offset + " length=" + length);
  }

  public object ReadStreamingObject()
      {
    if (_refs != null)
      _refs.Clear();

    return ReadObject();
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
  /// type ::= string
  /// type ::= int
  /// </code>
  /// </summary>
  public string ReadType()
      {
    int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();
    _offset--;

    switch (code) {
    case 0x00: case 0x01: case 0x02: case 0x03:
    case 0x04: case 0x05: case 0x06: case 0x07:
    case 0x08: case 0x09: case 0x0a: case 0x0b:
    case 0x0c: case 0x0d: case 0x0e: case 0x0f:

    case 0x10: case 0x11: case 0x12: case 0x13:
    case 0x14: case 0x15: case 0x16: case 0x17:
    case 0x18: case 0x19: case 0x1a: case 0x1b:
    case 0x1c: case 0x1d: case 0x1e: case 0x1f:

    case 0x30: case 0x31: case 0x32: case 0x33:
    case BC_STRING_CHUNK: case 'S':
      {
        string type = ReadString();

        if (_types == null)
          _types = new ArrayList();

        _types.Add(type);

        return type;
      }

    default:
      {
        int ref = ReadInt();

        if (_types.Size() <= ref)
          throw new IndexOutOfBoundsException("type ref #" + ref + " is greater than the number of valid types (" + _types.Size() + ")");

        return (String) _types.Get(ref);
      }
    }
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
    throw new NotSupportedException();
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
    int offset = _offset;
    
    if (offset + 3 < _length) {
      byte[] buffer = _buffer;
      
      int b32 = buffer[offset + 0] & 0xff;
      int b24 = buffer[offset + 1] & 0xff;
      int b16 = buffer[offset + 2] & 0xff;
      int b8 = buffer[offset + 3] & 0xff;

      _offset = offset + 4;

      return (b32 << 24) + (b24 << 16) + (b16 << 8) + b8;
    }
    else {
      int b32 = Read();
      int b24 = Read();
      int b16 = Read();
      int b8 = Read();

      return (b32 << 24) + (b24 << 16) + (b16 << 8) + b8;
    }
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

    return ((b64 << 56)
            + (b56 << 48)
            + (b48 << 40)
            + (b40 << 32)
            + (b32 << 24)
            + (b24 << 16)
            + (b16 << 8)
            + b8);
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
    long bits = ParseLong();
  
    return Double.LongBitsToDouble(bits);
  }

  org.w3c.dom.Node ParseXML()
      {
    throw new NotSupportedException();
  }

  private void ParseString(StringBuilder sbuf)
      {
    while (true) {
      if (_chunkLength <= 0) {
        if (! ParseChunkLength())
          return;
      }
      
      int length = _chunkLength;
      _chunkLength = 0;
      
      while (length-- > 0) {
        sbuf.Append((char) ParseUTF8Char());
      }
    }
  }

  /// <summary>
  /// Reads a character from the underlying stream.
  /// </summary>
  private int ParseChar()
      {
    while (_chunkLength <= 0) {
      if (! ParseChunkLength())
        return -1;
    }

    _chunkLength--;

    return ParseUTF8Char();
  }
  
  private bool ParseChunkLength()
      {
    if (_isLastChunk)
      return false;

    int code = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

    switch (code) {
    case BC_STRING_CHUNK:
      _isLastChunk = false;

      _chunkLength = (Read() << 8) + Read();
      break;
      
    case 'S':
      _isLastChunk = true;

      _chunkLength = (Read() << 8) + Read();
      break;

    case 0x00: case 0x01: case 0x02: case 0x03:
    case 0x04: case 0x05: case 0x06: case 0x07:
    case 0x08: case 0x09: case 0x0a: case 0x0b:
    case 0x0c: case 0x0d: case 0x0e: case 0x0f:

    case 0x10: case 0x11: case 0x12: case 0x13:
    case 0x14: case 0x15: case 0x16: case 0x17:
    case 0x18: case 0x19: case 0x1a: case 0x1b:
    case 0x1c: case 0x1d: case 0x1e: case 0x1f:
      _isLastChunk = true;
      _chunkLength = code - 0x00;
      break;

    case 0x30: case 0x31: case 0x32: case 0x33:
      _isLastChunk = true;
      _chunkLength = (code - 0x30)/// 256 + Read();
      break;

    default:
      throw Expect("string", code);
    }

    return true;
  }

  /// <summary>
  /// Parses a single UTF8 character.
  /// </summary>
  private int ParseUTF8Char()
      {
    int ch = _offset < _length ? (_buffer[_offset++] & 0xff) : Read();

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
      case BC_BINARY_CHUNK:
        _isLastChunk = false;

        _chunkLength = (Read() << 8) + Read();
        break;
        
      case 'B':
        _isLastChunk = true;

        _chunkLength = (Read() << 8) + Read();
        break;

      case 0x20: case 0x21: case 0x22: case 0x23:
      case 0x24: case 0x25: case 0x26: case 0x27:
      case 0x28: case 0x29: case 0x2a: case 0x2b:
      case 0x2c: case 0x2d: case 0x2e: case 0x2f:
        _isLastChunk = true;

        _chunkLength = code - 0x20;
        break;

      case 0x34: case 0x35: case 0x36: case 0x37:
        _isLastChunk = true;
        _chunkLength = (code - 0x34)/// 256 + Read();
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

    case BC_BINARY:
    case BC_BINARY_CHUNK:
      _isLastChunk = tag == BC_BINARY;
      _chunkLength = (Read() << 8) + Read();
      break;

    case 0x20: case 0x21: case 0x22: case 0x23:
    case 0x24: case 0x25: case 0x26: case 0x27:
    case 0x28: case 0x29: case 0x2a: case 0x2b:
    case 0x2c: case 0x2d: case 0x2e: case 0x2f:
      _isLastChunk = true;
      _chunkLength = tag - 0x20;
      break;

    case 0x34: case 0x35: case 0x36: case 0x37:
      _isLastChunk = true;
      _chunkLength = (tag - 0x34)/// 256 + Read();
      break;
      
    default:
      throw Expect("binary", tag);
    }
    
    return new ReadInputStream();
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
        case BC_BINARY_CHUNK:
          _isLastChunk = false;

          _chunkLength = (Read() << 8) + Read();
          break;
        
        case BC_BINARY:
          _isLastChunk = true;

          _chunkLength = (Read() << 8) + Read();
          break;

        case 0x20: case 0x21: case 0x22: case 0x23:
        case 0x24: case 0x25: case 0x26: case 0x27:
        case 0x28: case 0x29: case 0x2a: case 0x2b:
        case 0x2c: case 0x2d: case 0x2e: case 0x2f:
          _isLastChunk = true;
          _chunkLength = code - 0x20;
          break;

        case 0x34: case 0x35: case 0x36: case 0x37:
          _isLastChunk = true;
          _chunkLength = (code - 0x34)/// 256 + Read();
          break;

        default:
          throw Expect("byte[]", code);
        }
      }

      int sublen = _chunkLength;
      if (length < sublen)
        sublen = length;

      if (_length <= _offset && ! ReadBuffer())
        return -1;
      
      if (_length - _offset < sublen)
        sublen = _length - _offset;

      System.Arraycopy(_buffer, _offset, buffer, offset, sublen);

      _offset += sublen;
      
      offset += sublen;
      readLength += sublen;
      length -= sublen;
      _chunkLength -= sublen;
    }

    return readLength;
  }

  /// <summary>
  /// Normally, shouldn't be called externally, but needed for QA, e.g.
  /// ejb/3b01.
  /// </summary>
  public readonly int Read()
      {
    if (_length <= _offset && ! ReadBuffer())
      return -1;

    return _buffer[_offset++] & 0xff;
  }

  protected void Unread()
  {
    if (_offset <= 0)
      throw new IllegalStateException();
    
    _offset--;
  }

  private readonly bool ReadBuffer()
      {
    byte[] buffer = _buffer;
    int offset = _offset;
    int length = _length;
    
    if (offset < length) {
      System.Arraycopy(buffer, offset, buffer, 0, length - offset);
      offset = length - offset;
    }
    else
      offset = 0;
    
    int len = _is.Read(buffer, offset, SIZE - offset);

    if (len <= 0) {
      _length = offset;
      _offset = 0;
      
      return offset > 0;
    }

    _length = offset + len;
    _offset = 0;

    return true;
  }

  public Reader GetReader()
  {
    return null;
  }

  protected IOException Expect(string expect, int ch)
      {
    if (ch < 0)
      return Error("expected " + expect + " at end of file");
    else {
      _offset--;

      try {
        int offset = _offset;
        string context
          = BuildDebugContext(_buffer, 0, _length, offset);

        object obj = ReadObject();

        if (obj != null) {
          return Error("expected " + expect
                       + " at 0x" + Integer.ToHexString(ch & 0xff)
                       + " " + obj.GetType().Name + " (" + obj + ")"
                       + "\n  " + context + "");
        }
        else
          return Error("expected " + expect
                       + " at 0x" + Integer.ToHexString(ch & 0xff) + " null");
      } catch (Exception e) {
        log.Log(Level.FINE, e.ToString(), e);

        return Error("expected " + expect
                     + " at 0x" + Integer.ToHexString(ch & 0xff));
      }
    }
  }
  
  private string BuildDebugContext(byte[] buffer, int offset, int length,
                                   int errorOffset)
  {
    StringBuilder sb = new StringBuilder();
    
    sb.Append("[");
    for (int i = 0; i < errorOffset; i++) {
      int ch = buffer[offset + i];
      AddDebugChar(sb, ch);
    }
    sb.Append("] ");
    AddDebugChar(sb, buffer[offset + errorOffset]);
    sb.Append(" [");
    for (int i = errorOffset + 1; i < length; i++) {
      int ch = buffer[offset + i];
      AddDebugChar(sb, ch);
    }
    sb.Append("]");
    
    return sb.ToString();
  }
  
  private void AddDebugChar(StringBuilder sb, int ch)
  {    
    if (ch >= 0x20 && ch < 0x7f) {
      sb.Append((char) ch);
    }
    else if (ch == '\n')
      sb.Append((char) ch);
    else
      sb.Append(String.Format("\\x%02x", ch & 0xff));    
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
  
  public void Free()
  {
    Reset();
  }

    public override void Close()
      {
    InputStream is = _is;
    _is = null;

    if (_isCloseStreamOnClose && is != null)
      is.Close();
  }
  
  class ReadInputStream : InputStream {
    bool _isClosed = false;

    public int Read()
          {
      if (_isClosed)
        return -1;

      int ch = ParseByte();
      if (ch < 0)
        _isClosed = true;

      return ch;
    }

    public int Read(byte[] buffer, int offset, int length)
          {
      if (_isClosed)
        return -1;

      int len = Hessian2Input.this.Read(buffer, offset, length);
      if (len < 0)
        _isClosed = true;

      return len;
    }

    public void Close()
          {
      while (Read() >= 0) {
      }
    }
  };

  readonly static class ObjectDefinition {
    private readonly string _type;
    private readonly Deserializer _reader;
    private readonly object[] _fields;
    private readonly string[] _fieldNames;

    ObjectDefinition(string type,
                     Deserializer reader,
                     object[] fields,
                     string[] fieldNames)
    {
      _type = type;
      _reader = reader;
      _fields = fields;
      _fieldNames = fieldNames;
    }

    string GetType()
    {
      return _type;
    }

    Deserializer GetReader()
    {
      return _reader;
    }

    object[] GetFields()
    {
      return _fields;
    }
    
    string[] GetFieldNames()
    {
      return _fieldNames;
    }
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