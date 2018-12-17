using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Output stream for Hessian requests, compatible with microedition
/// Java.  It only uses classes and types available in JDK.
///
/// <para/>Since HessianOutput does not depend on any classes other than
/// in the JDK, it can be extracted independently into a smaller package.
///
/// <para/>HessianOutput is unbuffered, so any client needs to provide
/// its own buffering.
///
/// <code>
/// OutputStream os = ...; // from http connection
/// HessianOutput out = new HessianOutput(os);
/// string value;
///
/// out.StartCall("hello");  // start hello call
/// out.WriteString("arg1"); // write a string argument
/// out.CompleteCall();      // complete the call
/// </code>
/// </summary>
public class HessianOutput : AbstractHessianOutput {
  // the output stream/
  protected OutputStream os;
  // map of references
  private IdentityHashMap _refs;
  private int _version = 1;
  
  /// <summary>
  /// Creates a new Hessian output stream, initialized with an
  /// underlying output stream.
  ///
  /// <param name="os">the underlying output stream.</param>
  /// </summary>
  public HessianOutput(OutputStream os)
  {
    Init(os);
  }

  /// <summary>
  /// Creates an uninitialized Hessian output stream.
  /// </summary>
  public HessianOutput()
  {
  }

  /// <summary>
  /// Initializes the output
  /// </summary>
  public void Init(OutputStream os)
  {
    this.os = os;

    _refs = null;

    if (_serializerFactory == null)
      _serializerFactory = new SerializerFactory();
  }

  /// <summary>
  /// Sets the client's version.
  /// </summary>
  public void SetVersion(int version)
  {
    _version = version;
  }

  /// <summary>
  /// Writes a complete method call.
  /// </summary>
  public void Call(string method, object[] args)
      {
    int length = args != null ? args.length : 0;
    
    StartCall(method, length);
    
    for (int i = 0; i < length; i++)
      WriteObject(args[i]);
    
    CompleteCall();
  }

  /// <summary>
  /// Starts the method call.  Clients would use <code>startCall</code>
  /// instead of <code>call</code> if they wanted finer control over
  /// writing the arguments, or needed to write headers.
  ///
  /// <code><pre>
  /// c major minor
  /// m b16 b8 method-name
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
  public void StartCall(string method, int length)
      {
    os.Write('c');
    os.Write(_version);
    os.Write(0);

    os.Write('m');
    int len = method.Length();
    os.Write(len >> 8);
    os.Write(len);
    PrintString(method, 0, len);
  }

  /// <summary>
  /// Writes the call tag.  This would be followed by the
  /// headers and the method tag.
  ///
  /// <code><pre>
  /// c major minor
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
  public void StartCall()
      {
    os.Write('c');
    os.Write(0);
    os.Write(1);
  }

  /// <summary>
  /// Writes the method tag.
  ///
  /// <code><pre>
  /// m b16 b8 method-name
  /// </code></code>
  ///
  /// <param name="method">the method name to call.</param>
  /// </summary>
  public void WriteMethod(string method)
      {
    os.Write('m');
    int len = method.Length();
    os.Write(len >> 8);
    os.Write(len);
    PrintString(method, 0, len);
  }

  /// <summary>
  /// Completes.
  ///
  /// <code><pre>
  /// z
  /// </code></code>
  /// </summary>
  public void CompleteCall()
      {
    os.Write('z');
  }

  /// <summary>
  /// Starts the reply
  ///
  /// <para/>A successful completion will have a single value:
  ///
  /// <code>
  /// r
  /// </code>
  /// </summary>
  public void StartReply()
      {
    os.Write('r');
    os.Write(1);
    os.Write(0);
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
  public void CompleteReply()
      {
    os.Write('z');
  }

  /// <summary>
  /// Writes a header name.  The header value must immediately follow.
  ///
  /// <code><pre>
  /// H b16 b8 foo <em>value</em>
  /// </code></code>
  /// </summary>
  public void WriteHeader(string name)
      {
    int len = name.Length();
    
    os.Write('H');
    os.Write(len >> 8);
    os.Write(len);

    PrintString(name);
  }

  /// <summary>
  /// Writes a fault.  The fault will be written
  /// as a descriptive string followed by an object:
  ///
  /// <code><pre>
  /// f
  /// &lt;string>code
  /// &lt;string>the fault code
  ///
  /// &lt;string>message
  /// &lt;string>the fault mesage
  ///
  /// &lt;string>detail
  /// mt\x00\xnnjavax.ejb.FinderException
  ///     ...
  /// z
  /// z
  /// </code></code>
  ///
  /// <param name="code">the fault code, a three digit</param>
  /// </summary>
  public void WriteFault(string code, string message, object detail)
      {
    // hessian/3525
    os.Write('r');
    os.Write(1);
    os.Write(0);
    
    os.Write('f');
    WriteString("code");
    WriteString(code);

    WriteString("message");
    WriteString(message);

    if (detail != null) {
      WriteString("detail");
      WriteObject(detail);
    }
    os.Write('z');
    
    os.Write('z');
  }

  /// <summary>
  /// Writes any object to the output stream.
  /// </summary>
  public void WriteObject(object object)
      {
    if (object == null) {
      WriteNull();
      return;
    }

    ISerializer serializer;

    serializer = _serializerFactory.GetSerializer(object.GetClass());

    serializer.WriteObject(object, this);
  }

  /// <summary>
  /// Writes the list header to the stream.  List writers will call
  /// <code>writeListBegin</code> followed by the list contents and then
  /// call <code>writeListEnd</code>.
  ///
  /// <code><pre>
  /// V
  /// t b16 b8 type
  /// l b32 b24 b16 b8
  /// </code></code>
  /// </summary>
  public bool WriteListBegin(int length, string type)
      {
    os.Write('V');

    if (type != null) {
      os.Write('t');
      PrintLenString(type);
    }

    if (length >= 0) {
      os.Write('l');
      os.Write(length >> 24);
      os.Write(length >> 16);
      os.Write(length >> 8);
      os.Write(length);
    }

    return true;
  }

  /// <summary>
  /// Writes the tail of the list to the stream.
  /// </summary>
  public void WriteListEnd()
      {
    os.Write('z');
  }

  /// <summary>
  /// Writes the map header to the stream.  Map writers will call
  /// <code>writeMapBegin</code> followed by the map contents and then
  /// call <code>writeMapEnd</code>.
  ///
  /// <code><pre>
  /// Mt b16 b8 (<key> <value>)z
  /// </code></code>
  /// </summary>
  public void WriteMapBegin(string type)
      {
    os.Write('M');
    os.Write('t');
    PrintLenString(type);
  }

  /// <summary>
  /// Writes the tail of the map to the stream.
  /// </summary>
  public void WriteMapEnd()
      {
    os.Write('z');
  }

  /// <summary>
  /// Writes a remote object reference to the stream.  The type is the
  /// type of the remote interface.
  ///
  /// <code><pre>
  /// 'r' 't' b16 b8 type url
  /// </code></code>
  /// </summary>
  public void WriteRemote(string type, string url)
      {
    os.Write('r');
    os.Write('t');
    PrintLenString(type);
    os.Write('S');
    PrintLenString(url);
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
  public void WriteBoolean(bool value)
      {
    if (value)
      os.Write('T');
    else
      os.Write('F');
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
  public void WriteInt(int value)
      {
    os.Write('I');
    os.Write(value >> 24);
    os.Write(value >> 16);
    os.Write(value >> 8);
    os.Write(value);
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
    os.Write('L');
    os.Write((byte) (value >> 56));
    os.Write((byte) (value >> 48));
    os.Write((byte) (value >> 40));
    os.Write((byte) (value >> 32));
    os.Write((byte) (value >> 24));
    os.Write((byte) (value >> 16));
    os.Write((byte) (value >> 8));
    os.Write((byte) (value));
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
    long bits = Double.DoubleToLongBits(value);
    
    os.Write('D');
    os.Write((byte) (bits >> 56));
    os.Write((byte) (bits >> 48));
    os.Write((byte) (bits >> 40));
    os.Write((byte) (bits >> 32));
    os.Write((byte) (bits >> 24));
    os.Write((byte) (bits >> 16));
    os.Write((byte) (bits >> 8));
    os.Write((byte) (bits));
  }

  /// <summary>
  /// Writes a date to the stream.
  ///
  /// <code><pre>
  /// T  b64 b56 b48 b40 b32 b24 b16 b8
  /// </code></code>
  ///
  /// <param name="time">the date in milliseconds from the epoch in UTC</param>
  /// </summary>
  public void WriteUTCDate(long time)
      {
    os.Write('d');
    os.Write((byte) (time >> 56));
    os.Write((byte) (time >> 48));
    os.Write((byte) (time >> 40));
    os.Write((byte) (time >> 32));
    os.Write((byte) (time >> 24));
    os.Write((byte) (time >> 16));
    os.Write((byte) (time >> 8));
    os.Write((byte) (time));
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
    os.Write('N');
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
    if (value == null) {
      os.Write('N');
    }
    else {
      int length = value.Length();
      int offset = 0;
      
      while (length > 0x8000) {
        int sublen = 0x8000;

        // chunk can't end in high surrogate
        char tail = value.CharAt(offset + sublen - 1);

        if (0xd800 <= tail && tail <= 0xdbff)
          sublen--;
        
        os.Write('s');
        os.Write(sublen >> 8);
        os.Write(sublen);

        PrintString(value, offset, sublen);

        length -= sublen;
        offset += sublen;
      }

      os.Write('S');
      os.Write(length >> 8);
      os.Write(length);

      PrintString(value, offset, length);
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
      os.Write('N');
    }
    else {
      while (length > 0x8000) {
        int sublen = 0x8000;

        // chunk can't end in high surrogate
        char tail = buffer[offset + sublen - 1];

        if (0xd800 <= tail && tail <= 0xdbff)
          sublen--;
        
        os.Write('s');
        os.Write(sublen >> 8);
        os.Write(sublen);

        PrintString(buffer, offset, sublen);

        length -= sublen;
        offset += sublen;
      }

      os.Write('S');
      os.Write(length >> 8);
      os.Write(length);

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
    if (buffer == null)
      os.Write('N');
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
      os.Write('N');
    }
    else {
      while (length > 0x8000) {
        int sublen = 0x8000;
        
        os.Write('b');
        os.Write(sublen >> 8);
        os.Write(sublen);

        os.Write(buffer, offset, sublen);

        length -= sublen;
        offset += sublen;
      }

      os.Write('B');
      os.Write(length >> 8);
      os.Write(length);
      os.Write(buffer, offset, length);
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
      int sublen = length;

      if (0x8000 < sublen)
        sublen = 0x8000;

      os.Write('b');
      os.Write(sublen >> 8);
      os.Write(sublen);

      os.Write(buffer, offset, sublen);

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
  /// Writes a reference.
  ///
  /// <code><pre>
  /// R b32 b24 b16 b8
  /// </code></code>
  ///
  /// <param name="value">the integer value to write.</param>
  /// </summary>
  public void WriteRef(int value)
      {
    os.Write('R');
    os.Write(value >> 24);
    os.Write(value >> 16);
    os.Write(value >> 8);
    os.Write(value);
  }

  /// <summary>
  /// Writes a placeholder.
  ///
  /// <code><pre>
  /// P
  /// </code></code>
  /// </summary>
  public void WritePlaceholder()
      {
    os.Write('P');
  }

  /// <summary>
  /// If the object has already been written, just write its ref.
  ///
  /// <returns>true if we're writing a ref.</returns>
  /// </summary>
  public bool AddRef(object object)
      {
    if (_refs == null)
      _refs = new IdentityHashMap();

    Integer ref = (Integer) _refs.Get(object);

    if (ref != null) {
      int value = ref.IntValue();
      
      WriteRef(value);
      return true;
    }
    else {
      _refs.Put(object, new Integer(_refs.Size()));
      
      return false;
    }
  }
  
    public override int GetRef(object obj)
  {
    Integer value;
    
    if (_refs == null)
      return -1;
    
    value = (Integer) _refs.Get(obj);
    
    if (value == null)
      return -1;
    else
      return value;
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
  /// Removes a reference.
  /// </summary>
  public bool RemoveRef(object obj)
      {
    if (_refs != null) {
      _refs.Remove(obj);

      return true;
    }
    else
      return false;
  }

  /// <summary>
  /// Replaces a reference from one object to another.
  /// </summary>
  public bool ReplaceRef(object oldRef, object newRef)
      {
    Integer value = (Integer) _refs.Remove(oldRef);

    if (value != null) {
      _refs.Put(newRef, value);
      
      return true;
    }
    else
      return false;
  }

  /// <summary>
  /// Prints a string to the stream, encoded as UTF-8 with preceeding length
  ///
  /// <param name="v">the string to print.</param>
  /// </summary>
  public void PrintLenString(string v)
      {
    if (v == null) {
      os.Write(0);
      os.Write(0);
    }
    else {
      int len = v.Length();
      os.Write(len >> 8);
      os.Write(len);

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
  public void PrintString(string v, int offset, int length)
      {
    for (int i = 0; i < length; i++) {
      char ch = v.CharAt(i + offset);

      if (ch < 0x80)
        os.Write(ch);
      else if (ch < 0x800) {
        os.Write(0xc0 + ((ch >> 6) & 0x1f));
        os.Write(0x80 + (ch & 0x3f));
      }
      else {
        os.Write(0xe0 + ((ch >> 12) & 0xf));
        os.Write(0x80 + ((ch >> 6) & 0x3f));
        os.Write(0x80 + (ch & 0x3f));
      }
    }
  }
  
  /// <summary>
  /// Prints a string to the stream, encoded as UTF-8
  ///
  /// <param name="v">the string to print.</param>
  /// </summary>
  public void PrintString(char[] v, int offset, int length)
      {
    for (int i = 0; i < length; i++) {
      char ch = v[i + offset];

      if (ch < 0x80)
        os.Write(ch);
      else if (ch < 0x800) {
        os.Write(0xc0 + ((ch >> 6) & 0x1f));
        os.Write(0x80 + (ch & 0x3f));
      }
      else {
        os.Write(0xe0 + ((ch >> 12) & 0xf));
        os.Write(0x80 + ((ch >> 6) & 0x3f));
        os.Write(0x80 + (ch & 0x3f));
      }
    }
  }

  public void Flush()
      {
    if (this.os != null)
      this.os.Flush();
  }

  public void Close()
      {
    if (this.os != null)
      this.os.Flush();
  }
}

}