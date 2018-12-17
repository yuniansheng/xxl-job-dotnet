using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{









/// <summary>
/// Debugging output stream for Hessian requests.
/// </summary>
public class HessianDebugOutputStream : OutputStream
{
  private static readonly Logger log
    = Logger.GetLogger(HessianDebugOutputStream.class.GetName());
  
  private OutputStream _os;
  
  private HessianDebugState _state;
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugOutputStream(OutputStream os, PrintWriter dbg)
  {
    _os = os;

    _state = new HessianDebugState(dbg);
  }
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugOutputStream(OutputStream os, Logger log, Level level)
  {
    This(os, new PrintWriter(new LogWriter(log, level)));
  }
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugOutputStream(Logger log, Level level)
  {
    This(null, new PrintWriter(new LogWriter(log, level)));
  }
  
  public void InitPacket(OutputStream os)
  {
    _os = os;
  }

  public void StartTop2()
  {
    _state.StartTop2();
  }

  public void StartStreaming()
  {
    _state.StartStreaming();
  }

  /// <summary>
  /// Writes a character.
  /// </summary>
    public override void Write(int ch)
      {
    ch = ch & 0xff;
    
    _os.Write(ch);

    try {
      _state.Next(ch);
    } catch (Exception e) {
      log.Log(Level.WARNING, e.ToString(), e);
    }
  }

    public override void Flush()
      {
    _os.Flush();
  }

  /// <summary>
  /// closes the stream.
  /// </summary>
    public override void Close()
      {
    OutputStream os = _os;
    _os = null;

    if (os != null) {
      _state.Next(-1);
      os.Close();
    }

    _state.Println();
  }

  static class LogWriter : Writer {
    private Logger _log;
    private Level _level;
    private StringBuilder _sb = new StringBuilder();

    LogWriter(Logger log, Level level)
    {
      _log = log;
      _level = level;
    }

    public void Write(char ch)
    {
      if (ch == '\n' && _sb.Length() > 0) {
        _log.Log(_level, _sb.ToString());
        _sb.SetLength(0);
      }
      else
        _sb.Append((char) ch);
    }

    public void Write(char[] buffer, int offset, int length)
    {
      for (int i = 0; i < length; i++) {
        char ch = buffer[offset + i];

        if (ch == '\n' && _sb.Length() > 0) {
          _log.Log(_level, _sb.ToString());
          _sb.SetLength(0);
        }
        else
          _sb.Append((char) ch);
      }
    }

    public void Flush()
    {
    }

    public void Close()
    {
    }
  }
}

}