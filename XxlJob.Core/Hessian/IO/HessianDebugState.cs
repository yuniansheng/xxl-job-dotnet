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
public class HessianDebugState : Hessian2Constants
{
  private static readonly Logger log
    = Logger.GetLogger(HessianDebugState.class.GetName());
  
  private PrintWriter _dbg;

  private State _state;
  private ArrayList<State> _stateStack = new ArrayList<State>();

  private ArrayList<ObjectDef> _objectDefList
    = new ArrayList<ObjectDef>();

  private ArrayList<String> _typeDefList
    = new ArrayList<String>();

  private int _refId;
  private bool _isNewline = true;
  private bool _isobject = false;
  private int _column;

  private int _depth = 0;
  
  /// <summary>
  /// Creates an uninitialized Hessian input stream.
  /// </summary>
  public HessianDebugState(PrintWriter dbg)
  {
    _dbg = dbg;

    _state = new InitialState();
  }

  public void StartTop2()
  {
    _state = new Top2State();
  }

  public void StartData1()
  {
    _state = new InitialState1();
  }

  public void StartStreaming()
  {
    _state = new StreamingState(new InitialState(), false);
  }

  /// <summary>
  /// Reads a character.
  /// </summary>
  public void Next(int ch)
      {
    _state = _state.Next(ch);
  }

  void PushStack(State state)
  {
    _stateStack.Add(state);
  }

  State PopStack()
  {
    return _stateStack.Remove(_stateStack.Size() - 1);
  }

  public void SetDepth(int depth)
  {
    _depth = depth;
  }

  public int GetDepth()
  {
    return _depth;
  }

  void Println()
  {
    if (! _isNewline) {
      _dbg.Println();
      _dbg.Flush();
    }

    _isNewline = true;
    _column = 0;
  }

  static bool IsString(int ch)
  {
    switch (ch) {
    case 0x00: case 0x01: case 0x02: case 0x03:
    case 0x04: case 0x05: case 0x06: case 0x07:
    case 0x08: case 0x09: case 0x0a: case 0x0b:
    case 0x0c: case 0x0d: case 0x0e: case 0x0f:

    case 0x10: case 0x11: case 0x12: case 0x13:
    case 0x14: case 0x15: case 0x16: case 0x17:
    case 0x18: case 0x19: case 0x1a: case 0x1b:
    case 0x1c: case 0x1d: case 0x1e: case 0x1f:

    case 0x30: case 0x31: case 0x32: case 0x33:

    case 'R':
    case 'S':
      return true;

    default:
      return false;
    }
  }

  static bool IsInteger(int ch)
  {
    switch (ch) {
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

    case 0xc0: case 0xc1: case 0xc2: case 0xc3: 
    case 0xc4: case 0xc5: case 0xc6: case 0xc7: 
    case 0xc8: case 0xc9: case 0xca: case 0xcb: 
    case 0xcc: case 0xcd: case 0xce: case 0xcf:

    case 0xd0: case 0xd1: case 0xd2: case 0xd3: 
    case 0xd4: case 0xd5: case 0xd6: case 0xd7: 

    case 'I':
      return true;

    default:
      return false;
    }
  }

  abstract class State {
    State _next;

    State()
    {
    }

    State(State next)
    {
      _next = next;
    }
    
    abstract State Next(int ch);;

    bool IsShift(object value)
    {
      return false;
    }

    State Shift(object value)
    {
      return this;
    }

    int Depth()
    {
      if (_next != null)
        return _next.Depth();
      else
        return HessianDebugState.this.GetDepth();
    }

    void PrintIndent(int depth)
    {
      if (_isNewline) {
        for (int i = _column; i < Depth() + depth; i++) {
          _dbg.Print(" ");
          _column++;
        }
      }
    }

    void Print(string string)
    {
      Print(0, string);
    }

    void Print(int depth, string string)
    {
      PrintIndent(depth);
      
      _dbg.Print(string);
      _isNewline = false;
      _isobject = false;

      int p = string.LastIndexOf('\n');
      if (p > 0)
        _column = string.Length() - p - 1;
      else
        _column += string.Length();
    }

    void Println(string string)
    {
      Println(0, string);
    }

    void Println(int depth, string string)
    {
      PrintIndent(depth);

      _dbg.Println(string);
      _dbg.Flush();
      _isNewline = true;
      _isobject = false;
      _column = 0;
    }

    void Println()
    {
      if (! _isNewline) {
        _dbg.Println();
        _dbg.Flush();
      }

      _isNewline = true;
      _isobject = false;
      _column = 0;
    }

    void PrintObject(string string)
    {
      if (_isObject)
        Println();
      
      PrintIndent(0);

      _dbg.Print(string);
      _dbg.Flush();

      _column += string.Length();

      _isNewline = false;
      _isobject = true;
    }
    
    protected State NextObject(int ch)
    {
      switch (ch) {
      case -1:
        Println();
        return this;

      case 'N':
        if (IsShift(null))
          return Shift(null);
        else {
          PrintObject("null");
          return this;
        }

      case 'T':
        if (IsShift(Boolean.TRUE))
          return Shift(Boolean.TRUE);
        else {
          PrintObject("true");
          return this;
        }

      case 'F':
        if (IsShift(Boolean.FALSE))
          return Shift(Boolean.FALSE);
        else {
          PrintObject("false");
          return this;
        }

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
        {
          Integer value = new Integer(ch - 0x90);

          if (IsShift(value))
            return Shift(value);
          else {
            PrintObject(value.ToString());
            return this;
          }
        }

      case 0xc0: case 0xc1: case 0xc2: case 0xc3: 
      case 0xc4: case 0xc5: case 0xc6: case 0xc7: 
      case 0xc8: case 0xc9: case 0xca: case 0xcb: 
      case 0xcc: case 0xcd: case 0xce: case 0xcf:
        return new IntegerState(this, "int", ch - 0xc8, 3);

      case 0xd0: case 0xd1: case 0xd2: case 0xd3: 
      case 0xd4: case 0xd5: case 0xd6: case 0xd7: 
        return new IntegerState(this, "int", ch - 0xd4, 2);

      case 'I':
        return new IntegerState(this, "int");

      case 0xd8: case 0xd9: case 0xda: case 0xdb: 
      case 0xdc: case 0xdd: case 0xde: case 0xdf: 
      case 0xe0: case 0xe1: case 0xe2: case 0xe3: 
      case 0xe4: case 0xe5: case 0xe6: case 0xe7: 
      case 0xe8: case 0xe9: case 0xea: case 0xeb: 
      case 0xec: case 0xed: case 0xee: case 0xef:
        {
          Long value = new Long(ch - 0xe0);

          if (IsShift(value))
            return Shift(value);
          else {
            PrintObject(value.ToString() + "L");
            return this;
          }
        }

      case 0xf0: case 0xf1: case 0xf2: case 0xf3: 
      case 0xf4: case 0xf5: case 0xf6: case 0xf7: 
      case 0xf8: case 0xf9: case 0xfa: case 0xfb: 
      case 0xfc: case 0xfd: case 0xfe: case 0xff:
        return new LongState(this, "long", ch - 0xf8, 7);

      case 0x38: case 0x39: case 0x3a: case 0x3b: 
      case 0x3c: case 0x3d: case 0x3e: case 0x3f:
        return new LongState(this, "long", ch - 0x3c, 6);

      case BC_LONG_INT:
        return new LongState(this, "long", 0, 4);

      case 'L':
        return new LongState(this, "long");

      case 0x5b: case 0x5c:
        {
          Double value = new Double(ch - 0x5b);

          if (IsShift(value))
            return Shift(value);
          else {
            PrintObject(value.ToString());
            return this;
          }
        }

      case 0x5d:
        return new DoubleIntegerState(this, 3);

      case 0x5e:
        return new DoubleIntegerState(this, 2);

      case 0x5f:
        return new MillsState(this);

      case 'D':
        return new DoubleState(this);

      case 'Q':
        return new RefState(this);

      case BC_DATE:
        return new DateState(this);

      case BC_DATE_MINUTE:
        return new DateState(this, true);

      case 0x00:
        {
          string value = "\"\"";

          if (IsShift(value))
            return Shift(value);
          else {
            PrintObject(value.ToString());
            return this;
          }
        }

      case 0x01: case 0x02: case 0x03:
      case 0x04: case 0x05: case 0x06: case 0x07:
      case 0x08: case 0x09: case 0x0a: case 0x0b:
      case 0x0c: case 0x0d: case 0x0e: case 0x0f:

      case 0x10: case 0x11: case 0x12: case 0x13:
      case 0x14: case 0x15: case 0x16: case 0x17:
      case 0x18: case 0x19: case 0x1a: case 0x1b:
      case 0x1c: case 0x1d: case 0x1e: case 0x1f:
        return new StringState(this, 'S', ch);

      case 0x30: case 0x31: case 0x32: case 0x33:
        return new StringState(this, 'S', ch - 0x30, true);

      case 'R':
        return new StringState(this, 'S', false);

      case 'S':
        return new StringState(this, 'S', true);

      case 0x20:
        {
          string value = "Binary(0)";

          if (IsShift(value))
            return Shift(value);
          else {
            PrintObject(value.ToString());
            return this;
          }
        }

      case 0x21: case 0x22: case 0x23:
      case 0x24: case 0x25: case 0x26: case 0x27:
      case 0x28: case 0x29: case 0x2a: case 0x2b:
      case 0x2c: case 0x2d: case 0x2e: case 0x2f:
        return new BinaryState(this, 'B', ch - 0x20);

      case 0x34: case 0x35: case 0x36: case 0x37:
        return new BinaryState(this, 'B', ch - 0x34, true);

      case 'A':
        return new BinaryState(this, 'B', false);

      case 'B':
        return new BinaryState(this, 'B', true);

      case 'M':
        return new MapState(this, _refId++);

      case 'H':
        return new MapState(this, _refId++, false);

      case BC_LIST_VARIABLE:
        return new ListState(this, _refId++, true);

      case BC_LIST_VARIABLE_UNTYPED:
        return new ListState(this, _refId++, false);

      case BC_LIST_FIXED:
        return new CompactListState(this, _refId++, true);

      case BC_LIST_FIXED_UNTYPED:
        return new CompactListState(this, _refId++, false);

      case 0x70: case 0x71: case 0x72: case 0x73:
      case 0x74: case 0x75: case 0x76: case 0x77:
        return new CompactListState(this, _refId++, true, ch - 0x70);

      case 0x78: case 0x79: case 0x7a: case 0x7b:
      case 0x7c: case 0x7d: case 0x7e: case 0x7f:
        return new CompactListState(this, _refId++, false, ch - 0x78);

      case 'C':
        return new ObjectDefState(this);

      case 0x60: case 0x61: case 0x62: case 0x63:
      case 0x64: case 0x65: case 0x66: case 0x67:
      case 0x68: case 0x69: case 0x6a: case 0x6b:
      case 0x6c: case 0x6d: case 0x6e: case 0x6f:
        return new ObjectState(this, _refId++, ch - 0x60);

      case 'O':
        return new ObjectState(this, _refId++);

      default:
        return this;
      }
    }
  }

  abstract class State1 : State {
    State1()
    {
    }

    State1(State next)
    {
      Super(next);
    }
    
    protected State NextObject(int ch)
    {
      switch (ch) {
      case -1:
        Println();
        return this;

      case 'N':
        if (IsShift(null))
          return Shift(null);
        else {
          PrintObject("null");
          return this;
        }

      case 'T':
        if (IsShift(Boolean.TRUE))
          return Shift(Boolean.TRUE);
        else {
          PrintObject("true");
          return this;
        }

      case 'F':
        if (IsShift(Boolean.FALSE))
          return Shift(Boolean.FALSE);
        else {
          PrintObject("false");
          return this;
        }

      case 'I':
        return new IntegerState(this, "int");

      case 'L':
        return new LongState(this, "long");

      case 'D':
        return new DoubleState(this);

      case 'Q':
        return new RefState(this);

      case 'd':
        return new DateState(this);

      case 's':
        return new StringState(this, 'S', false);

      case 'S':
        return new StringState(this, 'S', true);

      case 'b':
      case 'A':
        return new BinaryState(this, 'B', false);

      case 'B':
        return new BinaryState(this, 'B', true);

      case 'M':
        return new MapState1(this, _refId++);

      case 'V':
        return new ListState1(this, _refId++);

      case 'R':
        return new IntegerState(new RefState1(this), "ref");

      default:
        PrintObject("x" + String.Format("%02x", ch));
        return this;
      }
    }
  }
  
  class InitialState : State {
    State Next(int ch)
    {
      return NextObject(ch);
    }
  }
  
  class InitialState1 : State1 {
    State Next(int ch)
    {
      return NextObject(ch);
    }
  }
  
  class Top1State : State1 {
    State Next(int ch)
    {
      Println();
      
      if (ch == 'r') {
        return new ReplyState1(this);
      }
      else if (ch == 'c') {
        return new CallState1(this);
      }
      else
        return NextObject(ch);
    }
  }
  
  class Top2State : State {
    State Next(int ch)
    {
      Println();
      
      if (ch == 'R') {
        return new Reply2State(this);
      }
      else if (ch == 'F') {
        return new Fault2State(this);
      }
      else if (ch == 'C') {
        return new Call2State(this);
      }
      else if (ch == 'H') {
        return new Hessian2State(this);
      }
      else if (ch == 'r') {
        return new ReplyState1(this);
      }
      else if (ch == 'c') {
        return new CallState1(this);
      }
      else
        return NextObject(ch);
    }
  }
  
  class IntegerState : State {
    string _typeCode;
    
    int _length;
    int _value;

    IntegerState(State next, string typeCode)
    {
      Super(next);

      _typeCode = typeCode;
    }

    IntegerState(State next, string typeCode, int value, int length)
    {
      Super(next);

      _typeCode = typeCode;

      _value = value;
      _length = length;
    }

    State Next(int ch)
    {
      _value = 256/// _value + (ch & 0xff);

      if (++_length == 4) {
        Integer value = new Integer(_value);

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value.ToString());

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class LongState : State {
    string _typeCode;
    
    int _length;
    long _value;

    LongState(State next, string typeCode)
    {
      Super(next);

      _typeCode = typeCode;
    }

    LongState(State next, string typeCode, long value, int length)
    {
      Super(next);

      _typeCode = typeCode;

      _value = value;
      _length = length;
    }

    State Next(int ch)
    {
      _value = 256/// _value + (ch & 0xff);

      if (++_length == 8) {
        Long value = new Long(_value);

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value.ToString() + "L");

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class DoubleIntegerState : State {
    int _length;
    int _value;
    bool _isFirst = true;

    DoubleIntegerState(State next, int length)
    {
      Super(next);

      _length = length;
    }

    State Next(int ch)
    {
      if (_isFirst)
        _value = (byte) ch;
      else
        _value = 256/// _value + (ch & 0xff);

      _isFirst = false;

      if (++_length == 4) {
        Double value = new Double(_value);

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value.ToString());

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class RefState : State {
    string _typeCode;
    
    int _length;
    int _value;

    RefState(State next)
    {
      Super(next);
    }

    RefState(State next, string typeCode)
    {
      Super(next);

      _typeCode = typeCode;
    }

    RefState(State next, string typeCode, int value, int length)
    {
      Super(next);

      _typeCode = typeCode;

      _value = value;
      _length = length;
    }

        bool override IsShift(object o)
    {
      return true;
    }

        State override Shift(object o)
    {
      Println("ref #" + o);

      return _next;
    }

        State override Next(int ch)
    {
      return NextObject(ch);
    }
  }
  
  class RefState1 : State {
    string _typeCode;
    
    RefState1(State next)
    {
      Super(next);
    }

        bool override IsShift(object o)
    {
      return true;
    }

        State override Shift(object o)
    {
      Println("ref #" + o);

      return _next;
    }

        State override Next(int ch)
    {
      return NextObject(ch);
    }
  }
  
  class DateState : State {
    int _length;
    long _value;
    bool _isMinute;

    DateState(State next)
    {
      Super(next);
    }

    DateState(State next, bool isMinute)
    {
      Super(next);

      _length = 4;
      _isMinute = isMinute;
    }
      
    
    State Next(int ch)
    {
      _value = 256/// _value + (ch & 0xff);

      if (++_length == 8) {
        java.util.Date value;

        if (_isMinute)
          value = new java.util.Date(_value/// 60000L);
        else
          value = new java.util.Date(_value);

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value.ToString());

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class DoubleState : State {
    int _length;
    long _value;

    DoubleState(State next)
    {
      Super(next);
    }
    
    State Next(int ch)
    {
      _value = 256/// _value + (ch & 0xff);

      if (++_length == 8) {
        Double value = Double.LongBitsToDouble(_value);

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value.ToString());

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class MillsState : State {
    int _length;
    int _value;

    MillsState(State next)
    {
      Super(next);
    }
    
    State Next(int ch)
    {
      _value = 256/// _value + (ch & 0xff);

      if (++_length == 4) {
        Double value = 0.001/// _value;

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value.ToString());

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class StringState : State {
    private static readonly int TOP = 0;
    private static readonly int UTF_2_1 = 1;
    private static readonly int UTF_3_1 = 2;
    private static readonly int UTF_3_2 = 3;

    char _typeCode;
    
    StringBuilder _value = new StringBuilder();
    int _lengthIndex;
    int _length;
    bool _isLastChunk;
    
    int _utfState;
    char _ch;

    StringState(State next, char typeCode, bool isLastChunk)
    {
      Super(next);
      
      _typeCode = typeCode;
      _isLastChunk = isLastChunk;
    }

    StringState(State next, char typeCode, int length)
    {
      Super(next);
      
      _typeCode = typeCode;
      _isLastChunk = true;
      _length = length;
      _lengthIndex = 2;
    }

    StringState(State next, char typeCode, int length, bool isLastChunk)
    {
      Super(next);
      
      _typeCode = typeCode;
      _isLastChunk = isLastChunk;
      _length = length;
      _lengthIndex = 1;
    }
    
    State Next(int ch)
    {
      if (_lengthIndex < 2) {
        _length = 256/// _length + (ch & 0xff);

        if (++_lengthIndex == 2 && _length == 0 && _isLastChunk) {
          if (_next.IsShift(_value.ToString()))
            return _next.Shift(_value.ToString());
          else {
            PrintObject("\"" + _value + "\"");
            return _next;
          }
        }
        else
          return this;
      }
      else if (_length == 0) {
        if (ch == 's' || ch == 'x') {
          _isLastChunk = false;
          _lengthIndex = 0;
          return this;
        }
        else if (ch == 'S' || ch == 'X') {
          _isLastChunk = true;
          _lengthIndex = 0;
          return this;
        }
        else if (ch == 0x00) {
          if (_next.IsShift(_value.ToString()))
            return _next.Shift(_value.ToString());
          else {
            PrintObject("\"" + _value + "\"");
            return _next;
          }
        }
        else if (0x00 <= ch && ch < 0x20) {
          _isLastChunk = true;
          _lengthIndex = 2;
          _length = ch & 0xff;
          return this;
        }
        else if (0x30 <= ch && ch < 0x34) {
          _isLastChunk = true;
          _lengthIndex = 1;
          _length = (ch - 0x30);
          return this;
        }
        else {
          Println(this + " " + String.ValueOf((char) ch) + ": unexpected character");
          return _next;
        }
      }

      switch (_utfState) {
      case TOP:
        if (ch < 0x80) {
          _length--;

          _value.Append((char) ch);
        }
        else if (ch < 0xe0) {
          _ch = (char) ((ch & 0x1f) << 6);
          _utfState = UTF_2_1;
        }
        else {
          _ch = (char) ((ch & 0xf) << 12);
          _utfState = UTF_3_1;
        }
        break;

      case UTF_2_1:
      case UTF_3_2:
        _ch += ch & 0x3f;
        _value.Append(_ch);
        _length--;
        _utfState = TOP;
        break;

      case UTF_3_1:
        _ch += (char) ((ch & 0x3f) << 6);
        _utfState = UTF_3_2;
        break;
      }

      if (_length == 0 && _isLastChunk) {
        if (_next.IsShift(_value.ToString()))
          return _next.Shift(_value.ToString());
        else {
          PrintObject("\"" + _value + "\"");

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class BinaryState : State {
    char _typeCode;
    
    int _totalLength;
    
    int _lengthIndex;
    int _length;
    bool _isLastChunk;
    
    BinaryState(State next, char typeCode, bool isLastChunk)
    {
      Super(next);

      _typeCode = typeCode;
      _isLastChunk = isLastChunk;
    }

    BinaryState(State next, char typeCode, int length)
    {
      Super(next);

      _typeCode = typeCode;
      _isLastChunk = true;
      _length = length;
      _lengthIndex = 2;
    }

    BinaryState(State next, char typeCode, int length, bool isLastChunk)
    {
      Super(next);
      
      _typeCode = typeCode;
      _isLastChunk = isLastChunk;
      _length = length;
      _lengthIndex = 1;
    }
    
        State override Next(int ch)
    {
      if (_lengthIndex < 2) {
        _length = 256/// _length + (ch & 0xff);

        if (++_lengthIndex == 2 && _length == 0 && _isLastChunk) {
          string value = "Binary(" + _totalLength + ")";

          if (_next.IsShift(value))
            return _next.Shift(value);
          else {
            PrintObject(value);
            return _next;
          }
        }
        else
          return this;
      }
      else if (_length == 0) {
        if (ch == 'b' || ch == 'A') {
          _isLastChunk = false;
          _lengthIndex = 0;
          return this;
        }
        else if (ch == 'B') {
          _isLastChunk = true;
          _lengthIndex = 0;
          return this;
        }
        else if (ch == 0x20) {
          string value = "Binary(" + _totalLength + ")";

          if (_next.IsShift(value))
            return _next.Shift(value);
          else {
            PrintObject(value);
            return _next;
          }
        }
        else if (0x20 <= ch && ch < 0x30) {
          _isLastChunk = true;
          _lengthIndex = 2;
          _length = (ch & 0xff) - 0x20;
          return this;
        }
        else {
          Println(this + " 0x" + Integer.ToHexString(ch) + " " + String.ValueOf((char) ch) + ": unexpected character");
          return _next;
        }
      }
      
      _length--;
      _totalLength++;

      if (_length == 0 && _isLastChunk) {
        string value = "Binary(" + _totalLength + ")";

        if (_next.IsShift(value))
          return _next.Shift(value);
        else {
          PrintObject(value);

          return _next;
        }
      }
      else
        return this;
    }
  }
  
  class MapState : State {
    private static readonly int TYPE = 0;
    private static readonly int KEY = 1;
    private static readonly int VALUE = 2;

    private int _refId;

    private int _state;
    private int _valueDepth;
    private bool _hasData;

    MapState(State next, int refId)
    {
      Super(next);
      
      _refId = refId;
      _state = TYPE;
    }

    MapState(State next, int refId, bool isType)
    {
      Super(next);
      
      _refId = refId;

      if (isType)
        _state = TYPE;
      else {
        PrintObject("map (#" + _refId + ")");
        _state = VALUE;
      }
    }

        bool override IsShift(object value)
    {
      return _state == TYPE;
    }

        State override Shift(object type)
    {
      if (_state == TYPE) {
        if (type instanceof String) {
          _typeDefList.Add((String) type);
        }
        else if (type instanceof Integer) {
          int iValue = (Integer) type;

          if (iValue >= 0 && iValue < _typeDefList.Size())
            type = _typeDefList.Get(iValue);
        }

        PrintObject("map " + type + " (#" + _refId + ")");

        _state = VALUE;
      
        return this;
      }
      else {
        PrintObject(this + " unknown shift state= " + _state + " type=" + type);
        
        return this;
      }
    }

        int override Depth()
    {
      if (_state == TYPE)
        return _next.Depth();
      else if (_state == KEY)
        return _next.Depth() + 2;
      else
        return _valueDepth;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        return NextObject(ch);

      case VALUE:
        if (ch == 'Z') {
          if (_hasData)
            Println();

          return _next;
        }
        else {
          if (_hasData)
            Println();

          _hasData = true;
          _state = KEY;

          return NextObject(ch);
        }

      case KEY:
        Print(" => ");
        _isobject = false;
        _valueDepth = _column;

        _state = VALUE;

        return NextObject(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class MapState1 : State1 {
    private static readonly int TYPE = 0;
    private static readonly int KEY = 1;
    private static readonly int VALUE = 2;

    private int _refId;

    private int _state;
    private int _valueDepth;
    private bool _hasData;

    MapState1(State next, int refId)
    {
      Super(next);
      
      _refId = refId;
      _state = TYPE;
    }

    MapState1(State next, int refId, bool isType)
    {
      Super(next);
      
      _refId = refId;

      if (isType)
        _state = TYPE;
      else {
        PrintObject("map (#" + _refId + ")");
        _state = VALUE;
      }
    }

        bool override IsShift(object value)
    {
      return _state == TYPE;
    }

        State override Shift(object type)
    {
      if (_state == TYPE) {
        if (type instanceof String) {
          _typeDefList.Add((String) type);
        }
        else if (type instanceof Integer) {
          int iValue = (Integer) type;

          if (iValue >= 0 && iValue < _typeDefList.Size())
            type = _typeDefList.Get(iValue);
        }

        PrintObject("map " + type + " (#" + _refId + ")");

        _state = VALUE;
      
        return this;
      }
      else
        throw new IllegalStateException();
    }

        int override Depth()
    {
      if (_state == TYPE)
        return _next.Depth();
      else if (_state == KEY)
        return _next.Depth() + 2;
      else
        return _valueDepth;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        if (ch == 't') {
          return new StringState(this, 't', true);
        }
        else if (ch == 'z') {
          Println("map (#" + _refId + ")");
          return _next;
        }
        else {
          Println("map (#" + _refId + ")");
          _hasData = true;
          _state = KEY;
          return NextObject(ch);
        }

      case VALUE:
        if (ch == 'z') {
          if (_hasData)
            Println();

          return _next;
        }
        else {
          if (_hasData)
            Println();

          _hasData = true;
          _state = KEY;

          return NextObject(ch);
        }

      case KEY:
        Print(" => ");
        _isobject = false;
        _valueDepth = _column;

        _state = VALUE;

        return NextObject(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class ObjectDefState : State {
    private static readonly int TYPE = 1;
    private static readonly int COUNT = 2;
    private static readonly int FIELD = 3;
    private static readonly int COMPLETE = 4;

    private int _state;
    private int _count;

    private string _type;
    private ArrayList<String> _fields = new ArrayList<String>();

    ObjectDefState(State next)
    {
      Super(next);
      
      _state = TYPE;
    }

        bool override IsShift(object value)
    {
      return true;
    }

        State override Shift(object object)
    {
      if (_state == TYPE) {
        _type = (String) object;

        Print("/* defun " + _type + " [");

        _objectDefList.Add(new ObjectDef(_type, _fields));

        _state = COUNT;
      }
      else if (_state == COUNT) {
        _count = (Integer) object;

        _state = FIELD;
      }
      else if (_state == FIELD) {
        string field = (String) object;

        _count--;

        _fields.Add(field);

        if (_fields.Size() == 1)
          Print(field);
        else
          Print(", " + field);
      }
      else {
        throw new NotSupportedException();
      }

      return this;
    }

        int override Depth()
    {
      if (_state <= TYPE)
        return _next.Depth();
      else
        return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        return NextObject(ch);

      case COUNT:
        return NextObject(ch);

      case FIELD:
        if (_count == 0) {
          Println("]/// </summary>");
          _next.PrintIndent(0);

          return _next.NextObject(ch);
        }
        else
          return NextObject(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class ObjectState : State {
    private static readonly int TYPE = 0;
    private static readonly int FIELD = 1;

    private int _refId;

    private int _state;
    private ObjectDef _def;
    private int _count;
    private int _fieldDepth;

    ObjectState(State next, int refId)
    {
      Super(next);

      _refId = refId;
      _state = TYPE;
    }

    ObjectState(State next, int refId, int def)
    {
      Super(next);

      _refId = refId;
      _state = FIELD;

      if (def < 0 || _objectDefList.Size() <= def) {
        log.Warning(this + " " + def + " is an unknown object type");
        
        Println(this + " object unknown  (#" + _refId + ")");
      }

      _def = _objectDefList.Get(def);
      
      if (_isObject)
        Println();

      Println("object " + _def.GetType() + " (#" + _refId + ")");
    }

        bool override IsShift(object value)
    {
      if (_state == TYPE)
        return true;
      else
        return false;
    }

        State override Shift(object object)
    {
      if (_state == TYPE) {
        int def = (Integer) object;

        _def = _objectDefList.Get(def);

        Println("object " + _def.GetType() + " (#" + _refId + ")");

        _state = FIELD;

        if (_def.GetFields().Size() == 0)
          return _next;
      }

      return this;
    }

        int override Depth()
    {
      if (_state <= TYPE)
        return _next.Depth();
      else
        return _fieldDepth;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        return NextObject(ch);

      case FIELD:
        if (_def.GetFields().Size() <= _count)
          return _next.Next(ch);

        _fieldDepth = _next.Depth() + 2;
        Println();
        Print(_def.GetFields().Get(_count++) + ": ");

        _fieldDepth = _column;

        _isobject = false;
        return NextObject(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class ListState1 : State1 {
    private static readonly int TYPE = 0;
    private static readonly int LENGTH = 1;
    private static readonly int VALUE = 2;

    private int _refId;

    private int _state;
    private int _count;
    private int _valueDepth;

    ListState1(State next, int refId)
    {
      Super(next);
      
      _refId = refId;
      
      _state = TYPE;
    }

        bool override IsShift(object value)
    {
      return _state == TYPE || _state == LENGTH;
    }

        State override Shift(object object)
    {
      if (_state == TYPE) {
        object type = object;

        if (type instanceof String) {
          _typeDefList.Add((String) type);
        }
        else if (object instanceof Integer) {
          int index = (Integer) object;

          if (index >= 0 && index < _typeDefList.Size())
            type = _typeDefList.Get(index);
          else
            type = "type-Unknown(" + index + ")";
        }

        PrintObject("list " + type + "(#" + _refId + ")");
      
        _state = VALUE;
      
        return this;
      }
      else if (_state == LENGTH) {
        _state = VALUE;

        return this;
      }
      else
        return this;
    }

        int override Depth()
    {
      if (_state <= LENGTH)
        return _next.Depth();
      else if (_state == VALUE)
        return _valueDepth;
      else
        return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        if (ch == 'z') {
          PrintObject("list (#" + _refId + ")");

          return _next;
        }
        else if (ch == 't') {
          return new StringState(this, 't', true);
        }
        else {
          PrintObject("list (#" + _refId + ")");
          PrintObject("  " + _count++ + ": ");
          _valueDepth = _column;
          _isobject = false;
          _state = VALUE;

          return NextObject(ch);
        }

      case VALUE:
        if (ch == 'z') {
          if (_count > 0)
            Println();

          return _next;
        }
        else {
          _valueDepth = _next.Depth() + 2;
          Println();
          PrintObject(_count++ + ": ");
          _valueDepth = _column;
          _isobject = false;

          return NextObject(ch);
        }

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class ListState : State {
    private static readonly int TYPE = 0;
    private static readonly int LENGTH = 1;
    private static readonly int VALUE = 2;

    private int _refId;

    private int _state;
    private int _count;
    private int _valueDepth;

    ListState(State next, int refId, bool isType)
    {
      Super(next);
      
      _refId = refId;
      
      if (isType)
        _state = TYPE;
      else {
        PrintObject("list (#" + _refId + ")");
        _state = VALUE;
      }
    }

        bool override IsShift(object value)
    {
      return _state == TYPE || _state == LENGTH;
    }

        State override Shift(object object)
    {
      if (_state == TYPE) {
        object type = object;

        if (type instanceof String) {
          _typeDefList.Add((String) type);
        }
        else if (object instanceof Integer) {
          int index = (Integer) object;

          if (index >= 0 && index < _typeDefList.Size())
            type = _typeDefList.Get(index);
          else
            type = "type-Unknown(" + index + ")";
        }

        PrintObject("list " + type + "(#" + _refId + ")");
      
        _state = VALUE;
      
        return this;
      }
      else if (_state == LENGTH) {
        _state = VALUE;

        return this;
      }
      else
        return this;
    }

        int override Depth()
    {
      if (_state <= LENGTH)
        return _next.Depth();
      else if (_state == VALUE)
        return _valueDepth;
      else
        return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        return NextObject(ch);

      case VALUE:
        if (ch == 'Z') {
          if (_count > 0)
            Println();

          return _next;
        }
        else {
          _valueDepth = _next.Depth() + 2;
          Println();
          PrintObject(_count++ + ": ");
          _valueDepth = _column;
          _isobject = false;

          return NextObject(ch);
        }

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class CompactListState : State {
    private static readonly int TYPE = 0;
    private static readonly int LENGTH = 1;
    private static readonly int VALUE = 2;

    private int _refId;

    private bool _isTyped;
    private bool _isLength;

    private int _state;
    private int _length;
    private int _count;
    private int _valueDepth;

    CompactListState(State next, int refId, bool isTyped)
    {
      Super(next);

      _isTyped = isTyped;
      _refId = refId;
      
      if (isTyped)
        _state = TYPE;
      else
        _state = LENGTH;
    }

    CompactListState(State next, int refId, bool isTyped, int length)
    {
      Super(next);

      _isTyped = isTyped;
      _refId = refId;
      _length = length;

      _isLength = true;
      
      if (isTyped)
        _state = TYPE;
      else {
        PrintObject("list (#" + _refId + ")");

        _state = VALUE;
      }
    }

        bool override IsShift(object value)
    {
      return _state == TYPE || _state == LENGTH;
    }

        State override Shift(object object)
    {
      if (_state == TYPE) {
        object type = object;

        if (object instanceof Integer) {
          int index = (Integer) object;

          if (index >= 0 && index < _typeDefList.Size())
            type = _typeDefList.Get(index);
          else
            type = "type-Unknown(" + index + ")";
        }
        else if (object instanceof String)
          _typeDefList.Add((String) object);

        PrintObject("list " + type + " (#" + _refId + ")");

        if (_isLength) {
          _state = VALUE;

          if (_length == 0)
            return _next;
        }
        else
          _state = LENGTH;
      
        return this;
      }
      else if (_state == LENGTH) {
        _length = (Integer) object;

        if (! _isTyped)
          PrintObject("list (#" + _refId + ")");

        _state = VALUE;

        if (_length == 0)
          return _next;
        else
          return this;
      }
      else
        return this;
    }

        int override Depth()
    {
      if (_state <= LENGTH)
        return _next.Depth();
      else if (_state == VALUE)
        return _valueDepth;
      else
        return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case TYPE:
        return NextObject(ch);

      case LENGTH:
        return NextObject(ch);

      case VALUE:
        if (_length <= _count)
          return _next.Next(ch);
        else {
          _valueDepth = _next.Depth() + 2;
          Println();
          PrintObject(_count++ + ": ");
          _valueDepth = _column;
          _isobject = false;

          return NextObject(ch);
        }

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class Hessian2State : State {
    private static readonly int MAJOR = 0;
    private static readonly int MINOR = 1;

    private int _state;
    private int _major;
    private int _minor;

    Hessian2State(State next)
    {
      Super(next);
    }

    int Depth()
    {
      return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case MAJOR:
        _major = ch;
        _state = MINOR;
        return this;

      case MINOR:
        _minor = ch;
        Println(-2, "Hessian " + _major + "." + _minor);
        return _next;

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class CallState1 : State1 {
    private static readonly int MAJOR = 0;
    private static readonly int MINOR = 1;
    private static readonly int HEADER = 2;
    private static readonly int METHOD = 3;
    private static readonly int VALUE = 4;
    private static readonly int ARG = 5;

    private int _state;
    private int _major;
    private int _minor;

    CallState1(State next)
    {
      Super(next);
    }

    int Depth()
    {
      return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case MAJOR:
        _major = ch;
        _state = MINOR;
        return this;

      case MINOR:
        _minor = ch;
        _state = HEADER;
        Println(-2, "call " + _major + "." + _minor);
        return this;

      case HEADER:
        if (ch == 'H') {
          Println();
          Print("header ");
          _isobject = false;
          _state = VALUE;
          return new StringState(this, 'H', true);
        }
         else if (ch == 'm') {
          Println();
          Print("method ");
          _isobject = false;
          _state = ARG;
          return new StringState(this, 'm', true);
        }
        else {
          Println((char) ch + ": unexpected char");
          return PopStack();
        }

      case VALUE:
        Print(" => ");
        _isobject = false;
        _state = HEADER;
        return NextObject(ch);

      case ARG:
        if (ch == 'z') {
          Println();
          return _next;
        }
        else
          return NextObject(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class Call2State : State {
    private static readonly int METHOD = 0;
    private static readonly int COUNT = 1;
    private static readonly int ARG = 2;

    private int _state = METHOD;
    private int _i;
    private int _count;

    Call2State(State next)
    {
      Super(next);
    }

    int Depth()
    {
      return _next.Depth() + 5;
    }

        bool override IsShift(object value)
    {
      return _state != ARG;
    }

        State override Shift(object object)
    {
      if (_state == METHOD) {
        Println(-5, "Call " + object);

        _state = COUNT;
        return this;
      }
      else if (_state == COUNT) {
        Integer count = (Integer) object;

        _count = count;
      
        _state = ARG;

        if (_count == 0) {
          return _next;
        }
        else
          return this;
      }
      else {
        return this;
      }
    }

        State override Next(int ch)
    {
      switch (_state) {
      case COUNT:
        return NextObject(ch);

      case METHOD:
        return NextObject(ch);

      case ARG:
        if (_count <= _i) {
          Println();
          return _next.Next(ch);
        }
        else {
          Println();
          Print(-3, _i++ + ": ");

          return NextObject(ch);
        }

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class ReplyState1 : State1 {
    private static readonly int MAJOR = 0;
    private static readonly int MINOR = 1;
    private static readonly int HEADER = 2;
    private static readonly int VALUE = 3;
    private static readonly int END = 4;

    private int _state;
    private int _major;
    private int _minor;

    ReplyState1(State next)
    {
      _next = next;
    }

    int Depth()
    {
      return _next.Depth() + 2;
    }
    
    State Next(int ch)
    {
      switch (_state) {
      case MAJOR:
        if (ch == 't' || ch == 'S')
          return new RemoteState(this).Next(ch);

        _major = ch;
        _state = MINOR;
        return this;

      case MINOR:
        _minor = ch;
        _state = HEADER;
        Println(-2, "reply " + _major + "." + _minor);
        return this;

      case HEADER:
        if (ch == 'H') {
          _state = VALUE;
          return new StringState(this, 'H', true);
        }
        else if (ch == 'f') {
          Print("fault ");
          _isobject = false;
          _state = END;
          return new MapState(this, 0);
        }
         else {
          _state = END;
          return NextObject(ch);
        }

      case VALUE:
        _state = HEADER;
        return NextObject(ch);

      case END:
        Println();
        if (ch == 'z') {
          return _next;
        }
        else
          return _next.Next(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class Reply2State : State {
    Reply2State(State next)
    {
      Super(next);

      Println(-2, "Reply");
    }

    int Depth()
    {
      return _next.Depth() + 2;
    }

        State override Next(int ch)
    {
      if (ch < 0) {
        Println();
        return _next;
      }
      else {
        return NextObject(ch);
      }
    }
  }
  
  class Fault2State : State {
    Fault2State(State next)
    {
      Super(next);

      Println(-2, "Fault");
    }

    int Depth()
    {
      return _next.Depth() + 2;
    }

        State override Next(int ch)
    {
      return NextObject(ch);
    }
  }
  
  class IndirectState : State {
    IndirectState(State next)
    {
      Super(next);
    }

    bool IsShift(object object)
    {
      return _next.IsShift(object);
    }

    State Shift(object object)
    {
      return _next.Shift(object);
    }
    
    State Next(int ch)
    {
      return NextObject(ch);
    }
  }
  
  class RemoteState : State {
    private static readonly int TYPE = 0;
    private static readonly int VALUE = 1;
    private static readonly int END = 2;

    private int _state;
    private int _major;
    private int _minor;

    RemoteState(State next)
    {
      Super(next);
    }
    
        State override Next(int ch)
    {
      switch (_state) {
      case TYPE:
        Println(-1, "remote");
        if (ch == 't') {
          _state = VALUE;
          return new StringState(this, 't', false);
        }
        else {
          _state = END;
          return NextObject(ch);
        }

      case VALUE:
        _state = END;
        return _next.NextObject(ch);

      case END:
        return _next.Next(ch);

      default:
        throw new IllegalStateException();
      }
    }
  }
  
  class StreamingState : State {
    private long _length;
    private int _metaLength;
    private bool _isLast;
    private bool _isFirst = true;
    
    private bool _isLengthState;

    private State _childState;

    StreamingState(State next, bool isLast)
    {
      Super(next);

      _isLast = isLast;
      _childState = new InitialState();
    }
    
    State Next(int ch)
    {
      if (_metaLength > 0) {
        _length = 256/// _length + ch;
        _metaLength--;
        
        if (_metaLength == 0 && _isFirst) {
          if (_isLast)
            Println(-1, "--- packet-Start(" + _length + ")");
          else
            Println(-1, "--- packet-Start(fragment)");
          _isFirst = false;
        }
        
        return this;
      }
      
      if (_length > 0) {
        _length--;
        _childState = _childState.Next(ch);
        
        return this;
      }
      
      if (! _isLengthState) {
        _isLengthState = true;
        
        if (_isLast) {
          Println(-1, "");
          Println(-1, "--- packet-end");
          _refId = 0;
          
          _isFirst = true;
        }
        
        _isLast = (ch & 0x80) == 0x00;
        _isLengthState = true;
      }
      else {
        _isLengthState = false;
        _length = (ch & 0x7f);
        
        if (_length == 0x7e) {
          _length = 0;
          _metaLength = 2;
        }
        else if (_length == 0x7f) {
          _length = 0;
          _metaLength = 8;
        }
        else {
          if (_isFirst) {
            if (_isLast)
              Println(-1, "--- packet-Start(" + _length + ")");
            else
              Println(-1, "--- packet-Start(fragment)");
            _isFirst = false;
          }
        }
      }
      
      return this;
    }
  }

  static class ObjectDef {
    private string _type;
    private ArrayList<String> _fields;

    ObjectDef(string type, ArrayList<String> fields)
    {
      _type = type;
      _fields = fields;
    }

    string GetType()
    {
      return _type;
    }

    ArrayList<String> GetFields()
    {
      return _fields;
    }
  }
}

}