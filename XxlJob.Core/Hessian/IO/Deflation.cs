using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{








public class Deflation : HessianEnvelope {
  public Deflation()
  {
  }

  public Hessian2Output Wrap(Hessian2Output out)
      {
    OutputStream os = new DeflateOutputStream(out);
    
    Hessian2Output filterOut = new Hessian2Output(os);

    filterOut.SetCloseStreamOnClose(true);
    
    return filterOut;
  }

  public Hessian2Input Unwrap(Hessian2Input in)
      {
    int version = in.ReadEnvelope();

    string method = in.ReadMethod();

    if (! method.Equals(GetType().Name))
      throw new IOException("expected hessian Envelope method '" +
                            GetType().Name + "' at '" + method + "'");

    return UnwrapHeaders(in);
  }

  public Hessian2Input UnwrapHeaders(Hessian2Input in)
      {
    InputStream is = new DeflateInputStream(in);

    Hessian2Input filter = new Hessian2Input(is);
    
    filter.SetCloseStreamOnClose(true);
    
    return filter;
  }
  
  static class DeflateOutputStream : OutputStream {
    private Hessian2Output _out;
    private OutputStream _bodyOut;
    private DeflaterOutputStream _deflateOut;
    
    DeflateOutputStream(Hessian2Output out)
          {
      _out = out;

      _out.StartEnvelope(Deflation.class.GetName());
    
      _out.WriteInt(0);

      _bodyOut = _out.GetBytesOutputStream();
    
      _deflateOut = new DeflaterOutputStream(_bodyOut);
    }
    
    public void Write(int ch)
          {
      _deflateOut.Write(ch);
    }
    
    public void Write(byte[] buffer, int offset, int length)
          {
      _deflateOut.Write(buffer, offset, length);
    }

    public void Close()
          {
      Hessian2Output out = _out;
      _out = null;

      if (out != null) {
        _deflateOut.Close();
        _bodyOut.Close();

        out.WriteInt(0);

        out.CompleteEnvelope();
          
        out.Close();
      }
    }
  }
  
  static class DeflateInputStream : InputStream {
    private Hessian2Input _in;
    
    private InputStream _bodyIn;
    private InflaterInputStream _inflateIn;
    
    DeflateInputStream(Hessian2Input in)
          {
      _in = in;

      int len = in.ReadInt();

      if (len != 0)
        throw new IOException("expected no headers");
      
      _bodyIn = _in.ReadInputStream();

      _inflateIn = new InflaterInputStream(_bodyIn);
    }
    
    public int Read()
          {
      return _inflateIn.Read();
    }
    
    public int Read(byte[] buffer, int offset, int length)
          {
      return _inflateIn.Read(buffer, offset, length);
    }

    public void Close()
          {
      Hessian2Input in = _in;
      _in = null;

      if (in != null) {
        _inflateIn.Close();
        _bodyIn.Close();

        int len = in.ReadInt();

        if (len != 0)
          throw new IOException("Unexpected footer");

        in.CompleteEnvelope();

        in.Close();
      }
    }
  }
}

}