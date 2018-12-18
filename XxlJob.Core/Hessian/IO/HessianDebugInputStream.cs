using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{











/// <summary>
/// Debugging input stream for Hessian requests.
/// </summary>
public class HessianDebugInputStream : InputStream
{
  private InputStream _is;
  
  private HessianDebugState _state;
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugInputStream(InputStream is, OutputStream os)
  {
    This(is, new PrintWriter(os));
  }
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugInputStream(InputStream is, PrintWriter dbg)
  {
    _is = is;

    if (dbg == null)
      dbg = new PrintWriter(System.out);

    _state = new HessianDebugState(dbg);
  }
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugInputStream(InputStream is, Logger log, Level level)
  {
    This(is, new PrintWriter(new LogWriter(log, level)));
  }
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugInputStream(Logger log, Level level)
  {
    This(null, log, level);
  }
  
  public void InitPacket(InputStream is)
  {
    _is = is;
  }

  public void StartTop2()
  {
    _state.StartTop2();
  }

  public void StartData1()
  {
    _state.StartData1();
  }
  
  public void StartStreaming()
  {
    _state.StartStreaming();
  }

  public void SetDepth(int depth)
  {
    _state.SetDepth(depth);
  }

  /// <summary>
  /// Reads a character.
  /// </summary>
  public int Read()
      {
    int ch;

    InputStream is = _is;

    if (is == null)
      return -1;
    else {
      ch = is.Read();
    }

    _state.Next(ch);

    return ch;
  }

  /// <summary>
  /// closes the stream.
  /// </summary>
  public void Close()
      {
    InputStream is = _is;
    _is = null;

    if (is != null)
      is.Close();
    
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