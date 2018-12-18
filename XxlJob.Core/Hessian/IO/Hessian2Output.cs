using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{








/// <summary>
/// Output stream for Hessian 2 requests.
///
/// <para/>Since HessianOutput does not depend on any classes other than
/// in the JDK, it can be extracted independently into a smaller package.
///
/// <para/>HessianOutput is unbuffered, so any client needs to provide
/// its own buffering.
///
/// <code>
/// OutputStream os = ...; // from http connection
/// Hessian2Output out = new Hessian2Output(os);
/// string value;
///
/// out.StartCall("hello", 1); // start hello call
/// out.WriteString("arg1");   // write a string argument
/// out.CompleteCall();        // complete the call
/// </code>
/// </summary>
public class Hessian2Output
  : AbstractHessianOutput
  : Hessian2Constants
{
  // should match Resin buffer size for perf
  public readonly static int SIZE = 8/// 1024;

  // the output stream/
  protected OutputStream _os;

  // map of references
  private readonly IdentityIntMap _refs
    = new IdentityIntMap(256);
  
  private int _refCount = 0;

  private bool _isCloseStreamOnClose;

  // map of classes
  private readonly IdentityIntMap _classRefs
    = new IdentityIntMap(256);

  // map of types
  private HashMap<String,Integer> _typeRefs;

  private readonly byte[] _buffer = new byte[SIZE];
  private int _offset;

  private bool _isPacket;
  
  private bool _isUnshared;

  /// <summary>
  /// Creates a new Hessian output stream, initialized with an
  /// underlying output stream.
  ///
  /// <param name="os">the underlying output stream.</param>
  /// </summary>
  public Hessian2Output()
  {
  }

  /// <summary>
  /// Creates a new Hessian output stream, initialized with an
  /// underlying output stream.
  ///
  /// <param name="os">the underlying output stream.</param>
  /// </summary>
  public Hessian2Output(OutputStream os)
  {
    Init(os);
  }

    public override void Init(OutputStream os)
  {
    Reset();

    _os = os;
  }

  public void InitPacket(OutputStream os)
  {
    ResetReferences();

    _os = os;
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
  /// Sets hessian to be "unshared", meaning it will not detect 
  /// duplicate or circular references.
  /// </summary>
    public override bool SetUnshared(bool isUnshared)
  {
    bool oldIsUnshared = _isUnshared;
    
    _isUnshared = isUnshared;
    
    return oldIsUnshared;
  }

  /// <summary>
  /// Writes a complete method call.
  /// </summary>
    public override void Call(string method, object[] args)
      {
    WriteVersion();

    int length = args != null ? args.length : 0;

    StartCall(method, length);

    for (int i = 0; i < length; i++) {
      WriteObject(args[i]);
    }

    CompleteCall();

    Flush();
  }

  /// <summary>
  /// Starts the method call.  Clients would use <code>startCall</code>
  /// instead of <code>call</code> if they wanted finer control over
  /// writing the arguments, or needed to write headers.
  ///
  /// <code><pre>
  /// C
  /// string # method name
  /// int    # arg count
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
    public override void StartCall(string method, int length)
      {
    int offset = _offset;

    if (SIZE < offset + 32) {
      FlushBuffer();
      offset = _offset;
    }

    byte[] buffer = _buffer;

    buffer[_offset++] = (byte) 'C';

    WriteString(method);
    WriteInt(length);
  }

  /// <summary>
  /// Writes the call tag.  This would be followed by the
  /// method and the arguments
  ///
  /// <code><pre>
  /// C
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
    public override void StartCall()
      {
    FlushIfFull();

    _buffer[_offset++] = (byte) 'C';
  }

  /// <summary>
  /// Starts an envelope.
  ///
  /// <code><pre>
  /// E major minor
  /// m b16 b8 method-name
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
  public void StartEnvelope(string method)
      {
    int offset = _offset;

    if (SIZE < offset + 32) {
      FlushBuffer();
      offset = _offset;
    }

    _buffer[_offset++] = (byte) 'E';

    WriteString(method);
  }

  /// <summary>
  /// Completes an envelope.
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// Z
  /// </code>
  /// </summary>
  public void CompleteEnvelope()
      {
    FlushIfFull();

    _buffer[_offset++] = (byte) 'Z';
  }

  /// <summary>
  /// Writes the method tag.
  ///
  /// <code><pre>
  /// string
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
  public void WriteMethod(string method)
      {
    WriteString(method);
  }

  /// <summary>
  /// Completes.
  ///
  /// <code><pre>
  /// z
  /// </code></code>
  /// </summary>
    public override void CompleteCall()
      {
    /*
    FlushIfFull();

    _buffer[_offset++] = (byte) 'Z';
   /// </summary>
  }

  /// <summary>
  /// Starts the reply
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// R
  /// </code>
  /// </summary>
    public override void StartReply()
      {
    WriteVersion();

    FlushIfFull();

    _buffer[_offset++] = (byte) 'R';
  }

  public void WriteVersion()
      {
    FlushIfFull();

    _buffer[_offset++] = (byte) 'H';
    _buffer[_offset++] = (byte) 2;
    _buffer[_offset++] = (byte) 0;
  }

  /// <summary>
  /// Completes reading the reply
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// z
  /// </code>
  /// </summary>
    public override void CompleteReply()
      {
  }

  /// <summary>
  /// Starts a packet
  ///
  /// <para/>A message contains several objects encapsulated by a length</p>
  ///
  /// <code>
  /// p x02 x00
  /// </code>
  /// </summary>
  public void StartMessage()
      {
    FlushIfFull();

    _buffer[_offset++] = (byte) 'p';
    _buffer[_offset++] = (byte) 2;
    _buffer[_offset++] = (byte) 0;
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
    FlushIfFull();

    _buffer[_offset++] = (byte) 'z';
  }

  /// <summary>
  /// Writes a fault.  The fault will be written
  /// as a descriptive string followed by an object:
  ///
  /// <code><pre>
  /// F map
  /// </code></code>
  ///
  /// <code><pre>
  /// F H
  /// \x04code
  /// \x10the fault code
  ///
  /// \x07message
  /// \x11the fault message
  ///
  /// \x06detail
  /// M\xnnjavax.ejb.FinderException
  ///     ...
  /// Z
  /// Z
  /// </code></code>
  ///
  /// <param name="code">the fault code, a three digit</param>
  /// </summary>
  public void WriteFault(string code, string message, object detail)
      {
    FlushIfFull();

    WriteVersion();

    _buffer[_offset++] = (byte) 'F';
    _buffer[_offset++] = (byte) 'H';

    AddRef(new Object(), _refCount++, false);

    WriteString("code");
    WriteString(code);

    WriteString("message");
    WriteString(message);

    if (detail != null) {
      WriteString("detail");
      WriteObject(detail);
    }

    FlushIfFull();
    _buffer[_offset++] = (byte) 'Z';
  }

  /// <summary>
  /// Writes any object to the output stream.
  /// </summary>
    public override void WriteObject(object object)
      {
    if (object == null) {
      WriteNull();
      return;
    }

    Serializer serializer
      = FindSerializerFactory().GetObjectSerializer(object.GetClass());

    serializer.WriteObject(object, this);
  }

  /// <summary>
  /// Writes the list header to the stream.  List writers will call
  /// <code>writeListBegin</code> followed by the list contents and then
  /// call <code>writeListEnd</code>.
  ///
  /// <code><pre>
  /// list ::= V type value* Z
  ///      ::= v type int value*
  /// </code></code>
  ///
  /// <returns>true for variable lists, false for fixed lists</returns>
  /// </summary>
  public bool WriteListBegin(int length, string type)
      {
    FlushIfFull();

    if (length < 0) {
      if (type != null) {
        _buffer[_offset++] = (byte) BC_LIST_VARIABLE;
        WriteType(type);
      }
      else
        _buffer[_offset++] = (byte) BC_LIST_VARIABLE_UNTYPED;

      return true;
    }
    else if (length <= LIST_DIRECT_MAX) {
      if (type != null) {
        _buffer[_offset++] = (byte) (BC_LIST_DIRECT + length);
        WriteType(type);
      }
      else {
        _buffer[_offset++] = (byte) (BC_LIST_DIRECT_UNTYPED + length);
      }

      return false;
    }
    else {
      if (type != null) {
        _buffer[_offset++] = (byte) BC_LIST_FIXED;
        WriteType(type);
      }
      else {
        _buffer[_offset++] = (byte) BC_LIST_FIXED_UNTYPED;
      }

      WriteInt(length);

      return false;
    }
  }

  /// <summary>
  /// Writes the tail of the list to the stream for a variable-length list.
  /// </summary>
  public void WriteListEnd()
      {
    FlushIfFull();

    _buffer[_offset++] = (byte) BC_END;
  }

  /// <summary>
  /// Writes the map header to the stream.  Map writers will call
  /// <code>writeMapBegin</code> followed by the map contents and then
  /// call <code>writeMapEnd</code>.
  ///
  /// <code><pre>
  /// map ::= M type (<value> <value>)* Z
  ///     ::= H (<value> <value>)* Z
  /// </code></code>
  /// </summary>
  public void WriteMapBegin(string type)
      {
    if (SIZE < _offset + 32)
      FlushBuffer();

    if (type != null) {
      _buffer[_offset++] = BC_MAP;

      WriteType(type);
    }
    else
      _buffer[_offset++] = BC_MAP_UNTYPED;
  }

  /// <summary>
  /// Writes the tail of the map to the stream.
  /// </summary>
  public void WriteMapEnd()
      {
    if (SIZE < _offset + 32)
      FlushBuffer();

    _buffer[_offset++] = (byte) BC_END;
  }

  /// <summary>
  /// Writes the object definition
  ///
  /// <code><pre>
  /// C &lt;string> &lt;int> &lt;string>*
  /// </code></code>
  /// </summary>
    public override int WriteObjectBegin(string type)
      {
    int newRef = _classRefs.Size();
    int ref = _classRefs.Put(type, newRef, false);

    if (newRef != ref) {
      if (SIZE < _offset + 32)
        FlushBuffer();

      if (ref <= OBJECT_DIRECT_MAX) {
        _buffer[_offset++] = (byte) (BC_OBJECT_DIRECT + ref);
      }
      else {
        _buffer[_offset++] = (byte) 'O';
        WriteInt(ref);
      }

      return ref;
    }
    else {
      if (SIZE < _offset + 32)
        FlushBuffer();

      _buffer[_offset++] = (byte) 'C';

      WriteString(type);

      return -1;
    }
  }

  /// <summary>
  /// Writes the tail of the class definition to the stream.
  /// </summary>
    public override void WriteClassFieldLength(int len)
      {
    WriteInt(len);
  }

  /// <summary>
  /// Writes the tail of the object definition to the stream.
  /// </summary>
    public override void WriteObjectEnd()
      {
  }

  /// <summary>
  /// <code><pre>
  /// type ::= string
  ///      ::= int
  /// </code></pre>
  /// </summary>
  private void WriteType(string type)
      {
    FlushIfFull();

    int len = type.Length();
    if (len == 0) {
      throw new IllegalArgumentException("empty type is not allowed");
    }

    if (_typeRefs == null)
      _typeRefs = new HashMap<String,Integer>();

    Integer typeRefV = (Integer) _typeRefs.Get(type);

    if (typeRefV != null) {
      int typeRef = typeRefV.IntValue();

      WriteInt(typeRef);
    }
    else {
      _typeRefs.Put(type, Integer.ValueOf(_typeRefs.Size()));

      WriteString(type);
    }
  }

  /// <summary>
  /// Writes a bool value to the stream.  The bool will be written
  /// with the following syntax:
  ///
  /// <code><pre>
  /// T
  /// F
  /// </code></code>
  ///
  /// <param name="value">the bool value to write.</param>
  /// </summary>
    public override void WriteBoolean(bool value)
      {
    if (SIZE < _offset + 16)
      FlushBuffer();

    if (value)
      _buffer[_offset++] = (byte) 'T';
    else
      _buffer[_offset++] = (byte) 'F';
  }

  /// <summary>
  /// Writes an integer value to the stream.  The integer will be written
  /// with the following syntax:
  ///
  /// <code><pre>
  /// I b32 b24 b16 b8
  /// </code></code>
  ///
  /// <param name="value">the integer value to write.</param>
  /// </summary>
    public override void WriteInt(int value)
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    if (SIZE <= offset + 16) {
      FlushBuffer();
      offset = _offset;
    }

    if (INT_DIRECT_MIN <= value && value <= INT_DIRECT_MAX)
      buffer[offset++] = (byte) (value + BC_INT_ZERO);
    else if (INT_BYTE_MIN <= value && value <= INT_BYTE_MAX) {
      buffer[offset++] = (byte) (BC_INT_BYTE_ZERO + (value >> 8));
      buffer[offset++] = (byte) (value);
    }
    else if (INT_SHORT_MIN <= value && value <= INT_SHORT_MAX) {
      buffer[offset++] = (byte) (BC_INT_SHORT_ZERO + (value >> 16));
      buffer[offset++] = (byte) (value >> 8);
      buffer[offset++] = (byte) (value);
    }
    else {
      buffer[offset++] = (byte) ('I');
      buffer[offset++] = (byte) (value >> 24);
      buffer[offset++] = (byte) (value >> 16);
      buffer[offset++] = (byte) (value >> 8);
      buffer[offset++] = (byte) (value);
    }

    _offset = offset;
  }

  /// <summary>
  /// Writes a long value to the stream.  The long will be written
  /// with the following syntax:
  ///
  /// <code><pre>
  /// L b64 b56 b48 b40 b32 b24 b16 b8
  /// </code></code>
  ///
  /// <param name="value">the long value to write.</param>
  /// </summary>
  public void WriteLong(long value)
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    if (SIZE <= offset + 16) {
      FlushBuffer();
      offset = _offset;
    }

    if (LONG_DIRECT_MIN <= value && value <= LONG_DIRECT_MAX) {
      buffer[offset++] = (byte) (value + BC_LONG_ZERO);
    }
    else if (LONG_BYTE_MIN <= value && value <= LONG_BYTE_MAX) {
      buffer[offset++] = (byte) (BC_LONG_BYTE_ZERO + (value >> 8));
      buffer[offset++] = (byte) (value);
    }
    else if (LONG_SHORT_MIN <= value && value <= LONG_SHORT_MAX) {
      buffer[offset++] = (byte) (BC_LONG_SHORT_ZERO + (value >> 16));
      buffer[offset++] = (byte) (value >> 8);
      buffer[offset++] = (byte) (value);
    }
    else if (-0x80000000L <= value && value <= 0x7fffffffL) {
      buffer[offset + 0] = (byte) BC_LONG_INT;
      buffer[offset + 1] = (byte) (value >> 24);
      buffer[offset + 2] = (byte) (value >> 16);
      buffer[offset + 3] = (byte) (value >> 8);
      buffer[offset + 4] = (byte) (value);

      offset += 5;
    }
    else {
      buffer[offset + 0] = (byte) 'L';
      buffer[offset + 1] = (byte) (value >> 56);
      buffer[offset + 2] = (byte) (value >> 48);
      buffer[offset + 3] = (byte) (value >> 40);
      buffer[offset + 4] = (byte) (value >> 32);
      buffer[offset + 5] = (byte) (value >> 24);
      buffer[offset + 6] = (byte) (value >> 16);
      buffer[offset + 7] = (byte) (value >> 8);
      buffer[offset + 8] = (byte) (value);

      offset += 9;
    }

    _offset = offset;
  }

  /// <summary>
  /// Writes a double value to the stream.  The double will be written
  /// with the following syntax:
  ///
  /// <code><pre>
  /// D b64 b56 b48 b40 b32 b24 b16 b8
  /// </code></code>
  ///
  /// <param name="value">the double value to write.</param>
  /// </summary>
  public void WriteDouble(double value)
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    if (SIZE <= offset + 16) {
      FlushBuffer();
      offset = _offset;
    }

    int intValue = (int) value;

    if (intValue == value) {
      if (intValue == 0) {
        buffer[offset++] = (byte) BC_DOUBLE_ZERO;

        _offset = offset;

        return;
      }
      else if (intValue == 1) {
        buffer[offset++] = (byte) BC_DOUBLE_ONE;

        _offset = offset;

        return;
      }
      else if (-0x80 <= intValue && intValue < 0x80) {
        buffer[offset++] = (byte) BC_DOUBLE_BYTE;
        buffer[offset++] = (byte) intValue;

        _offset = offset;

        return;
      }
      else if (-0x8000 <= intValue && intValue < 0x8000) {
        buffer[offset + 0] = (byte) BC_DOUBLE_SHORT;
        buffer[offset + 1] = (byte) (intValue >> 8);
        buffer[offset + 2] = (byte) intValue;

        _offset = offset + 3;

        return;
      }
    }

    int mills = (int) (value/// 1000);

    if (0.001/// mills == value) {
      buffer[offset + 0] = (byte) (BC_DOUBLE_MILL);
      buffer[offset + 1] = (byte) (mills >> 24);
      buffer[offset + 2] = (byte) (mills >> 16);
      buffer[offset + 3] = (byte) (mills >> 8);
      buffer[offset + 4] = (byte) (mills);

      _offset = offset + 5;

      return;
    }

    long bits = Double.DoubleToLongBits(value);

    buffer[offset + 0] = (byte) 'D';
    buffer[offset + 1] = (byte) (bits >> 56);
    buffer[offset + 2] = (byte) (bits >> 48);
    buffer[offset + 3] = (byte) (bits >> 40);
    buffer[offset + 4] = (byte) (bits >> 32);
    buffer[offset + 5] = (byte) (bits >> 24);
    buffer[offset + 6] = (byte) (bits >> 16);
    buffer[offset + 7] = (byte) (bits >> 8);
    buffer[offset + 8] = (byte) (bits);

    _offset = offset + 9;
  }

  /// <summary>
  /// Writes a date to the stream.
  ///
  /// <code><pre>
  /// date ::= d   b7 b6 b5 b4 b3 b2 b1 b0
  ///      ::= x65 b3 b2 b1 b0
  /// </code></code>
  ///
  /// <param name="time">the date in milliseconds from the epoch in UTC</param>
  /// </summary>
  public void WriteUTCDate(long time)
      {
    if (SIZE < _offset + 32)
      FlushBuffer();

    int offset = _offset;
    byte[] buffer = _buffer;

    if (time % 60000L == 0) {
      // compact date ::= x65 b3 b2 b1 b0

      long minutes = time / 60000L;

      if ((minutes >> 31) == 0 || (minutes >> 31) == -1) {
        buffer[offset++] = (byte) BC_DATE_MINUTE;
        buffer[offset++] = ((byte) (minutes >> 24));
        buffer[offset++] = ((byte) (minutes >> 16));
        buffer[offset++] = ((byte) (minutes >> 8));
        buffer[offset++] = ((byte) (minutes >> 0));

        _offset = offset;
        return;
      }
    }

    buffer[offset++] = (byte) BC_DATE;
    buffer[offset++] = ((byte) (time >> 56));
    buffer[offset++] = ((byte) (time >> 48));
    buffer[offset++] = ((byte) (time >> 40));
    buffer[offset++] = ((byte) (time >> 32));
    buffer[offset++] = ((byte) (time >> 24));
    buffer[offset++] = ((byte) (time >> 16));
    buffer[offset++] = ((byte) (time >> 8));
    buffer[offset++] = ((byte) (time));

    _offset = offset;
  }

  /// <summary>
  /// Writes a null value to the stream.
  /// The null will be written with the following syntax
  ///
  /// <code><pre>
  /// N
  /// </code></code>
  ///
  /// <param name="value">the string value to write.</param>
  /// </summary>
  public void WriteNull()
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    if (SIZE <= offset + 16) {
      FlushBuffer();
      offset = _offset;
    }

    buffer[offset++] = 'N';

    _offset = offset;
  }

  /// <summary>
  /// Writes a string value to the stream using UTF-8 encoding.
  /// The string will be written with the following syntax:
  ///
  /// <code><pre>
  /// S b16 b8 string-value
  /// </code></code>
  ///
  /// If the value is null, it will be written as
  ///
  /// <code><pre>
  /// N
  /// </code></code>
  ///
  /// <param name="value">the string value to write.</param>
  /// </summary>
  public void WriteString(string value)
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    if (SIZE <= offset + 16) {
      FlushBuffer();
      offset = _offset;
    }

    if (value == null) {
      buffer[offset++] = (byte) 'N';

      _offset = offset;
    }
    else {
      int length = value.Length();
      int strOffset = 0;

      while (length > 0x8000) {
        int sublen = 0x8000;

        offset = _offset;

        if (SIZE <= offset + 16) {
          FlushBuffer();
          offset = _offset;
        }

        // chunk can't end in high surrogate
        char tail = value.CharAt(strOffset + sublen - 1);

        if (0xd800 <= tail && tail <= 0xdbff)
          sublen--;

        buffer[offset + 0] = (byte) BC_STRING_CHUNK;
        buffer[offset + 1] = (byte) (sublen >> 8);
        buffer[offset + 2] = (byte) (sublen);

        _offset = offset + 3;

        PrintString(value, strOffset, sublen);

        length -= sublen;
        strOffset += sublen;
      }

      offset = _offset;

      if (SIZE <= offset + 16) {
        FlushBuffer();
        offset = _offset;
      }

      if (length <= STRING_DIRECT_MAX) {
        buffer[offset++] = (byte) (BC_STRING_DIRECT + length);
      }
      else if (length <= STRING_SHORT_MAX) {
        buffer[offset++] = (byte) (BC_STRING_SHORT + (length >> 8));
        buffer[offset++] = (byte) (length);
      }
      else {
        buffer[offset++] = (byte) ('S');
        buffer[offset++] = (byte) (length >> 8);
        buffer[offset++] = (byte) (length);
      }

      _offset = offset;

      PrintString(value, strOffset, length);
    }
  }

  /// <summary>
  /// Writes a string value to the stream using UTF-8 encoding.
  /// The string will be written with the following syntax:
  ///
  /// <code><pre>
  /// S b16 b8 string-value
  /// </code></code>
  ///
  /// If the value is null, it will be written as
  ///
  /// <code><pre>
  /// N
  /// </code></code>
  ///
  /// <param name="value">the string value to write.</param>
  /// </summary>
  public void WriteString(char[] buffer, int offset, int length)
      {
    if (buffer == null) {
      if (SIZE < _offset + 16)
        FlushBuffer();

      _buffer[_offset++] = (byte) ('N');
    }
    else {
      while (length > 0x8000) {
        int sublen = 0x8000;

        if (SIZE < _offset + 16)
          FlushBuffer();

        // chunk can't end in high surrogate
        char tail = buffer[offset + sublen - 1];

        if (0xd800 <= tail && tail <= 0xdbff)
          sublen--;

        _buffer[_offset++] = (byte) BC_STRING_CHUNK;
        _buffer[_offset++] = (byte) (sublen >> 8);
        _buffer[_offset++] = (byte) (sublen);

        PrintString(buffer, offset, sublen);

        length -= sublen;
        offset += sublen;
      }

      if (SIZE < _offset + 16)
        FlushBuffer();

      if (length <= STRING_DIRECT_MAX) {
        _buffer[_offset++] = (byte) (BC_STRING_DIRECT + length);
      }
      else if (length <= STRING_SHORT_MAX) {
        _buffer[_offset++] = (byte) (BC_STRING_SHORT + (length >> 8));
        _buffer[_offset++] = (byte) length;
      }
      else {
        _buffer[_offset++] = (byte) ('S');
        _buffer[_offset++] = (byte) (length >> 8);
        _buffer[_offset++] = (byte) (length);
      }

      PrintString(buffer, offset, length);
    }
  }

  /// <summary>
  /// Writes a byte array to the stream.
  /// The array will be written with the following syntax:
  ///
  /// <code><pre>
  /// B b16 b18 bytes
  /// </code></code>
  ///
  /// If the value is null, it will be written as
  ///
  /// <code><pre>
  /// N
  /// </code></code>
  ///
  /// <param name="value">the string value to write.</param>
  /// </summary>
  public void WriteBytes(byte[] buffer)
      {
    if (buffer == null) {
      if (SIZE < _offset + 16)
        FlushBuffer();

      _buffer[_offset++] = 'N';
    }
    else
      WriteBytes(buffer, 0, buffer.length);
  }

  /// <summary>
  /// Writes a byte array to the stream.
  /// The array will be written with the following syntax:
  ///
  /// <code><pre>
  /// B b16 b18 bytes
  /// </code></code>
  ///
  /// If the value is null, it will be written as
  ///
  /// <code><pre>
  /// N
  /// </code></code>
  ///
  /// <param name="value">the string value to write.</param>
  /// </summary>
  public void WriteBytes(byte[] buffer, int offset, int length)
      {
    if (buffer == null) {
      if (SIZE < _offset + 16)
        FlushBuffer();

      _buffer[_offset++] = (byte) 'N';
    }
    else {
      while (SIZE - _offset - 3 < length) {
        int sublen = SIZE - _offset - 3;

        if (sublen < 16) {
          FlushBuffer();

          sublen = SIZE - _offset - 3;

          if (length < sublen)
            sublen = length;
        }

        _buffer[_offset++] = (byte) BC_BINARY_CHUNK;
        _buffer[_offset++] = (byte) (sublen >> 8);
        _buffer[_offset++] = (byte) sublen;

        System.Arraycopy(buffer, offset, _buffer, _offset, sublen);
        _offset += sublen;

        length -= sublen;
        offset += sublen;

        FlushBuffer();
      }

      if (SIZE < _offset + 16)
        FlushBuffer();

      if (length <= BINARY_DIRECT_MAX) {
        _buffer[_offset++] = (byte) (BC_BINARY_DIRECT + length);
      }
      else if (length <= BINARY_SHORT_MAX) {
        _buffer[_offset++] = (byte) (BC_BINARY_SHORT + (length >> 8));
        _buffer[_offset++] = (byte) (length);
      }
      else {
        _buffer[_offset++] = (byte) 'B';
        _buffer[_offset++] = (byte) (length >> 8);
        _buffer[_offset++] = (byte) (length);
      }

      System.Arraycopy(buffer, offset, _buffer, _offset, length);

      _offset += length;
    }
  }

  /// <summary>
  /// Writes a byte buffer to the stream.
  ///
  /// <code><pre>
  /// </code></code>
  /// </summary>
  public void WriteByteBufferStart()
      {
  }

  /// <summary>
  /// Writes a byte buffer to the stream.
  ///
  /// <code><pre>
  /// b b16 b18 bytes
  /// </code></code>
  /// </summary>
  public void WriteByteBufferPart(byte[] buffer, int offset, int length)
      {
    while (length > 0) {
      FlushIfFull();

      int sublen = _buffer.length - _offset;

      if (length < sublen)
        sublen = length;

      _buffer[_offset++] = BC_BINARY_CHUNK;
      _buffer[_offset++] = (byte) (sublen >> 8);
      _buffer[_offset++] = (byte) sublen;

      System.Arraycopy(buffer, offset, _buffer, _offset, sublen);

     _offset += sublen;
      length -= sublen;
      offset += sublen;
    }
  }

  /// <summary>
  /// Writes a byte buffer to the stream.
  ///
  /// <code><pre>
  /// b b16 b18 bytes
  /// </code></code>
  /// </summary>
  public void WriteByteBufferEnd(byte[] buffer, int offset, int length)
      {
    WriteBytes(buffer, offset, length);
  }

  /// <summary>
  /// Returns an output stream to write binary data.
  /// </summary>
  public OutputStream GetBytesOutputStream()
      {
    return new BytesOutputStream();
  }

  /// <summary>
  /// Writes a full output stream.
  /// </summary>
    public override void WriteByteStream(InputStream is)
      {
    while (true) {
      int len = SIZE - _offset - 3;

      if (len < 16) {
        FlushBuffer();
        len = SIZE - _offset - 3;
      }

      len = is.Read(_buffer, _offset + 3, len);

      if (len <= 0) {
        _buffer[_offset++] = BC_BINARY_DIRECT;
        return;
      }

      _buffer[_offset + 0] = (byte) BC_BINARY_CHUNK;
      _buffer[_offset + 1] = (byte) (len >> 8);
      _buffer[_offset + 2] = (byte) (len);

      _offset += len + 3;
    }
  }

  /// <summary>
  /// Writes a reference.
  ///
  /// <code><pre>
  /// x51 &lt;int>
  /// </code></code>
  ///
  /// <param name="value">the integer value to write.</param>
  /// </summary>
    protected override void WriteRef(int value)
      {
    if (SIZE < _offset + 16)
      FlushBuffer();

    _buffer[_offset++] = (byte) BC_REF;

    WriteInt(value);
  }

  /// <summary>
  /// If the object has already been written, just write its ref.
  ///
  /// <returns>true if we're writing a ref.</returns>
  /// </summary>
    public override bool AddRef(object object)
      {
    if (_isUnshared) {
      _refCount++;
      return false;
    }
    
    int newRef = _refCount;

    int ref = AddRef(object, newRef, false);
    
    if (ref != newRef) {
      WriteRef(ref);

      return true;
    }
    else {
      _refCount++;
      
      return false;
    }
  }
  
    public override int GetRef(object obj)
  {
    if (_isUnshared)
      return -1;
    
    return _refs.Get(obj);
  }

  /// <summary>
  /// Removes a reference.
  /// </summary>
    public override bool RemoveRef(object obj)
      {
    if (_isUnshared) {
      return false;
    }
    else if (_refs != null) {
      _refs.Remove(obj);

      return true;
    }
    else
      return false;
  }

  /// <summary>
  /// Replaces a reference from one object to another.
  /// </summary>
    public override bool ReplaceRef(object oldRef, object newRef)
      {
    if (_isUnshared) {
      return false;
    }
    
    int value = _refs.Get(oldRef);

    if (value >= 0) {
      AddRef(newRef, value, true);
      
      _refs.Remove(oldRef);
      
      return true;
    }
    else
      return false;
  }
  
  private int AddRef(object value, int newRef, bool isReplace)
  {
    int prevRef = _refs.Put(value, newRef, isReplace);
    
    return prevRef;
  }

  /// <summary>
  /// Starts the streaming message
  ///
  /// <para/>A streaming message starts with 'P'</p>
  ///
  /// <code>
  /// P x02 x00
  /// </code>
  /// </summary>
  public void WriteStreamingObject(object obj)
      {
    StartPacket();

    WriteObject(obj);

    EndPacket();
  }

  /// <summary>
  /// Starts a streaming packet
  ///
  /// <para/>A streaming contains a set of chunks, ending with a zero chunk.
  /// Each chunk is a length followed by data where the length is
  /// encoded by (b1xxxxxxxx)* b0xxxxxxxx</p>
  /// </summary>
  public void StartPacket()
      {
    if (_refs != null) {
      _refs.Clear();
      _refCount = 0;
    }

    FlushBuffer();

    _isPacket = true;
    _offset = 4;
    _buffer[0] = (byte) 0x05; // 0x05 = binary
    _buffer[1] = (byte) 0x55;
    _buffer[2] = (byte) 0x55;
    _buffer[3] = (byte) 0x55;
  }

  public void EndPacket()
      {
    int offset = _offset;

    OutputStream os = _os;

    if (os == null) {
      _offset = 0;
      return;
    }

    int len = offset - 4;

    if (len < 0x7e) {
      _buffer[2] = _buffer[0];
      _buffer[3] = (byte) (len);
    } else {
      _buffer[1] = (byte) (0x7e);
      _buffer[2] = (byte) (len >> 8);
      _buffer[3] = (byte) (len);
    }

    _isPacket = false;
    _offset = 0;

    if (os == null) {
    }
    else if (len < 0x7e) {
      os.Write(_buffer, 2, offset - 2);
    }
    else {
      os.Write(_buffer, 0, offset);
    }
  }

  /// <summary>
  /// Prints a string to the stream, encoded as UTF-8 with preceeding length
  ///
  /// <param name="v">the string to print.</param>
  /// </summary>
  public void PrintLenString(string v)
      {
    if (SIZE < _offset + 16)
      FlushBuffer();

    if (v == null) {
      _buffer[_offset++] = (byte) (0);
      _buffer[_offset++] = (byte) (0);
    }
    else {
      int len = v.Length();
      _buffer[_offset++] = (byte) (len >> 8);
      _buffer[_offset++] = (byte) (len);

      PrintString(v, 0, len);
    }
  }

  /// <summary>
  /// Prints a string to the stream, encoded as UTF-8
  ///
  /// <param name="v">the string to print.</param>
  /// </summary>
  public void PrintString(string v)
      {
    PrintString(v, 0, v.Length());
  }

  /// <summary>
  /// Prints a string to the stream, encoded as UTF-8
  ///
  /// <param name="v">the string to print.</param>
  /// </summary>
  public void PrintString(string v, int strOffset, int length)
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    for (int i = 0; i < length; i++) {
      if (SIZE <= offset + 16) {
        _offset = offset;
        FlushBuffer();
        offset = _offset;
      }

      char ch = v.CharAt(i + strOffset);

      if (ch < 0x80)
        buffer[offset++] = (byte) (ch);
      else if (ch < 0x800) {
        buffer[offset++] = (byte) (0xc0 + ((ch >> 6) & 0x1f));
        buffer[offset++] = (byte) (0x80 + (ch & 0x3f));
      }
      else {
        buffer[offset++] = (byte) (0xe0 + ((ch >> 12) & 0xf));
        buffer[offset++] = (byte) (0x80 + ((ch >> 6) & 0x3f));
        buffer[offset++] = (byte) (0x80 + (ch & 0x3f));
      }
    }

    _offset = offset;
  }

  /// <summary>
  /// Prints a string to the stream, encoded as UTF-8
  ///
  /// <param name="v">the string to print.</param>
  /// </summary>
  public void PrintString(char[] v, int strOffset, int length)
      {
    int offset = _offset;
    byte[] buffer = _buffer;

    for (int i = 0; i < length; i++) {
      if (SIZE <= offset + 16) {
        _offset = offset;
        FlushBuffer();
        offset = _offset;
      }

      char ch = v[i + strOffset];

      if (ch < 0x80)
        buffer[offset++] = (byte) (ch);
      else if (ch < 0x800) {
        buffer[offset++] = (byte) (0xc0 + ((ch >> 6) & 0x1f));
        buffer[offset++] = (byte) (0x80 + (ch & 0x3f));
      }
      else {
        buffer[offset++] = (byte) (0xe0 + ((ch >> 12) & 0xf));
        buffer[offset++] = (byte) (0x80 + ((ch >> 6) & 0x3f));
        buffer[offset++] = (byte) (0x80 + (ch & 0x3f));
      }
    }

    _offset = offset;
  }

  private readonly void FlushIfFull()
      {
    int offset = _offset;

    if (SIZE < offset + 32) {
      FlushBuffer();
    }
  }

  public readonly void Flush()
      {
    FlushBuffer();

    if (_os != null)
      _os.Flush();
  }

  public readonly void FlushBuffer()
      {
    int offset = _offset;

    OutputStream os = _os;

    if (! _isPacket && offset > 0) {
      _offset = 0;
      if (os != null)
        os.Write(_buffer, 0, offset);
    }
    else if (_isPacket && offset > 4) {
      int len = offset - 4;
      
      _buffer[0] |= (byte) 0x80;
      _buffer[1] = (byte) (0x7e);
      _buffer[2] = (byte) (len >> 8);
      _buffer[3] = (byte) (len);
      _offset = 4;

      if (os != null)
        os.Write(_buffer, 0, offset);

      _buffer[0] = (byte) 0x00;
      _buffer[1] = (byte) 0x56;
      _buffer[2] = (byte) 0x56;
      _buffer[3] = (byte) 0x56;
    }
  }

    public override void Close()
      {
    // hessian/3a8c
    Flush();

    OutputStream os = _os;
    _os = null;

    if (os != null) {
      if (_isCloseStreamOnClose)
        os.Close();
    }
  }

  public void Free()
  {
    Reset();

    _os = null;
    _isCloseStreamOnClose = false;
  }

  /// <summary>
  /// Resets the references for streaming.
  /// </summary>
    public override void ResetReferences()
  {
    if (_refs != null) {
      _refs.Clear();
      _refCount = 0;
    }
  }

  /// <summary>
  /// Resets all counters and references
  /// </summary>
  public void Reset()
  {
    if (_refs != null) {
      _refs.Clear();
      _refCount = 0;
    }

    _classRefs.Clear();
    _typeRefs = null;
    _offset = 0;
    _isPacket = false;
    _isUnshared = false;
  }

  class BytesOutputStream : OutputStream {
    private int _startOffset;

    BytesOutputStream()
          {
      if (SIZE < _offset + 16) {
        Hessian2Output.this.FlushBuffer();
      }

      _startOffset = _offset;
      _offset += 3; // skip 'b' xNN xNN
    }

        public override void Write(int ch)
          {
      if (SIZE <= _offset) {
        int length = (_offset - _startOffset) - 3;

        _buffer[_startOffset] = (byte) BC_BINARY_CHUNK;
        _buffer[_startOffset + 1] = (byte) (length >> 8);
        _buffer[_startOffset + 2] = (byte) (length);

        Hessian2Output.this.FlushBuffer();

        _startOffset = _offset;
        _offset += 3;
      }

      _buffer[_offset++] = (byte) ch;
    }

        public override void Write(byte[] buffer, int offset, int length)
          {
      while (length > 0) {
        int sublen = SIZE - _offset;

        if (length < sublen)
          sublen = length;

        if (sublen > 0) {
          System.Arraycopy(buffer, offset, _buffer, _offset, sublen);
          _offset += sublen;
        }

        length -= sublen;
        offset += sublen;

        if (SIZE <= _offset) {
          int chunkLength = (_offset - _startOffset) - 3;

          _buffer[_startOffset] = (byte) BC_BINARY_CHUNK;
          _buffer[_startOffset + 1] = (byte) (chunkLength >> 8);
          _buffer[_startOffset + 2] = (byte) (chunkLength);

          Hessian2Output.this.FlushBuffer();

          _startOffset = _offset;
          _offset += 3;
        }
      }
    }

        public override void Close()
          {
      int startOffset = _startOffset;
      _startOffset = -1;

      if (startOffset < 0)
        return;

      int length = (_offset - startOffset) - 3;

      _buffer[startOffset] = (byte) 'B';
      _buffer[startOffset + 1] = (byte) (length >> 8);
      _buffer[startOffset + 2] = (byte) (length);

      Hessian2Output.this.FlushBuffer();
    }
  }
}

}