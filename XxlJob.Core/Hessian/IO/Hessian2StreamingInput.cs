using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{






/// <summary>
/// Input stream for Hessian 2 streaming requests using WebSocket.
/// 
/// For best performance, use HessianFactory:
/// 
/// <code><pre>
/// HessianFactory factory = new HessianFactory();
/// Hessian2StreamingInput hIn = factory.CreateHessian2StreamingInput(is);
/// </code></code>
/// </summary>
public class Hessian2StreamingInput
{
  private static readonly Logger log
    = Logger.GetLogger(Hessian2StreamingInput.class.GetName());
  
  private StreamingInputStream _is;
  private Hessian2Input _in;
  
  /// <summary>
  /// Creates a new Hessian input stream, initialized with an
  /// underlying input stream.
  ///
  /// <param name="is">the underlying output stream.</param>
  /// </summary>
  public Hessian2StreamingInput(InputStream is)
  {
    _is = new StreamingInputStream(is);
    _in = new Hessian2Input(_is);
  }

  public void SetSerializerFactory(SerializerFactory factory)
  {
    _in.SetSerializerFactory(factory);
  }

  public bool IsDataAvailable()
  {
    StreamingInputStream is = _is;
    
    return is != null && is.IsDataAvailable();
  }

  public Hessian2Input StartPacket()
      {
    if (_is.StartPacket()) {
      _in.ResetReferences();
      _in.ResetBuffer(); // XXX:
      return _in;
    }
    else
      return null;
  }

  public void EndPacket()
      {
    _is.EndPacket();
    _in.ResetBuffer(); // XXX:
  }

  public Hessian2Input GetHessianInput()
  {
    return _in;
  }

  /// <summary>
  /// Read the next object
  /// </summary>
  public object ReadObject()
      {
    _is.StartPacket();
    
    object obj = _in.ReadStreamingObject();

    _is.EndPacket();

    return obj;
  }

  /// <summary>
  /// Close the output.
  /// </summary>
  public void Close()
      {
    _in.Close();
  }

  static class StreamingInputStream : InputStream {
    private InputStream _is;
    
    private int _length;
    private bool _isPacketEnd;

    StreamingInputStream(InputStream is)
    {
      _is = is;
    }

    public bool IsDataAvailable()
    {
      try {
        return _is != null && _is.Available() > 0;
      } catch (IOException e) {
        log.Log(Level.FINER, e.ToString(), e);

        return true;
      }
    }

    public bool StartPacket()
          {
      // skip zero-length packets
      do {
        _isPacketEnd = false;
      } while ((_length = ReadChunkLength(_is)) == 0);

      return _length > 0;
    }

    public void EndPacket()
          {
      while (! _isPacketEnd) {
        if (_length <= 0)
          _length = ReadChunkLength(_is);

        if (_length > 0) {
          _is.Skip(_length);
          _length = 0;
        }
      }
      
      if (_length > 0) {
        _is.Skip(_length);
        _length = 0;
      }
    }

    public int Read()
          {
      InputStream is = _is;
      
      if (_length == 0) {
        if (_isPacketEnd)
          return -1;
        
        _length = ReadChunkLength(is);

        if (_length <= 0)
          return -1;
      }

      _length--;
      
      return is.Read();
    }

        public override int Read(byte[] buffer, int offset, int length)
          {
      InputStream is = _is;
      
      if (_length <= 0) {
        if (_isPacketEnd)
          return -1;
        
        _length = ReadChunkLength(is);

        if (_length <= 0)
          return -1;
      }

      int sublen = _length;
      if (length < sublen)
        sublen = length;
      
      sublen = is.Read(buffer, offset, sublen);

      if (sublen < 0)
        return -1;

      _length -= sublen;

      return sublen;
    }

    private int ReadChunkLength(InputStream is)
          {
      if (_isPacketEnd)
        return -1;
      
      int length = 0;

      int code = is.Read();

      if (code < 0) {
        _isPacketEnd = true;
        return -1;
      }
      
      _isPacketEnd = (code & 0x80) == 0;
      
      int len = is.Read() & 0x7f;
      
      if (len < 0x7e) {
        length = len;
      }
      else if (len == 0x7e) {
        length = (((is.Read() & 0xff) << 8)
                  + (is.Read() & 0xff));
      }
      else {
        length = (((is.Read() & 0xff) << 56)
                  + ((is.Read() & 0xff) << 48)
                  + ((is.Read() & 0xff) << 40)
                  + ((is.Read() & 0xff) << 32)
                  + ((is.Read() & 0xff) << 24)
                  + ((is.Read() & 0xff) << 16)
                  + ((is.Read() & 0xff) << 8)
                  + ((is.Read() & 0xff)));
      }

      return length;
    }
  }
}

}