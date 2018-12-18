using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{

public interface Hessian2Constants
{
  public static readonly int BC_BINARY = 'B'; // readonly chunk
  public static readonly int BC_BINARY_CHUNK = 'A'; // non-readonly chunk
  public static readonly int BC_BINARY_DIRECT = 0x20; // 1-byte length binary
  public static readonly int BINARY_DIRECT_MAX = 0x0f;
  public static readonly int BC_BINARY_SHORT = 0x34; // 2-byte length binary
  public static readonly int BINARY_SHORT_MAX = 0x3ff; // 0-1023 binary

  public static readonly int BC_CLASS_DEF = 'C'; // object/class definition

  public static readonly int BC_DATE = 0x4a; // 64-bit millisecond UTC date
  public static readonly int BC_DATE_MINUTE = 0x4b; // 32-bit minute UTC date
  
  public static readonly int BC_DOUBLE = 'D'; // IEEE 64-bit double

  public static readonly int BC_DOUBLE_ZERO = 0x5b;
  public static readonly int BC_DOUBLE_ONE = 0x5c;
  public static readonly int BC_DOUBLE_BYTE = 0x5d;
  public static readonly int BC_DOUBLE_SHORT = 0x5e;
  public static readonly int BC_DOUBLE_MILL = 0x5f;
  
  public static readonly int BC_FALSE = 'F'; // bool false
  
  public static readonly int BC_INT = 'I'; // 32-bit int
  
  public static readonly int INT_DIRECT_MIN = -0x10;
  public static readonly int INT_DIRECT_MAX = 0x2f;
  public static readonly int BC_INT_ZERO = 0x90;

  public static readonly int INT_BYTE_MIN = -0x800;
  public static readonly int INT_BYTE_MAX = 0x7ff;
  public static readonly int BC_INT_BYTE_ZERO = 0xc8;
  
  public static readonly int BC_END = 'Z';

  public static readonly int INT_SHORT_MIN = -0x40000;
  public static readonly int INT_SHORT_MAX = 0x3ffff;
  public static readonly int BC_INT_SHORT_ZERO = 0xd4;

  public static readonly int BC_LIST_VARIABLE =0x55;
  public static readonly int BC_LIST_FIXED = 'V';
  public static readonly int BC_LIST_VARIABLE_UNTYPED = 0x57;
  public static readonly int BC_LIST_FIXED_UNTYPED =0x58;

  public static readonly int BC_LIST_DIRECT = 0x70;
  public static readonly int BC_LIST_DIRECT_UNTYPED = 0x78;
  public static readonly int LIST_DIRECT_MAX = 0x7;

  public static readonly int BC_LONG = 'L'; // 64-bit signed integer
  public static readonly long LONG_DIRECT_MIN = -0x08;
  public static readonly long LONG_DIRECT_MAX =  0x0f;
  public static readonly int BC_LONG_ZERO = 0xe0;

  public static readonly long LONG_BYTE_MIN = -0x800;
  public static readonly long LONG_BYTE_MAX =  0x7ff;
  public static readonly int BC_LONG_BYTE_ZERO = 0xf8;

  public static readonly int LONG_SHORT_MIN = -0x40000;
  public static readonly int LONG_SHORT_MAX = 0x3ffff;
  public static readonly int BC_LONG_SHORT_ZERO = 0x3c;
  
  public static readonly int BC_LONG_INT = 0x59;
  
  public static readonly int BC_MAP = 'M';
  public static readonly int BC_MAP_UNTYPED = 'H';
  
  public static readonly int BC_NULL = 'N';
  
  public static readonly int BC_OBJECT = 'O';
  public static readonly int BC_OBJECT_DEF = 'C';
  
  public static readonly int BC_OBJECT_DIRECT = 0x60;
  public static readonly int OBJECT_DIRECT_MAX = 0x0f;
  
  public static readonly int BC_REF = 0x51;

  public static readonly int BC_STRING = 'S'; // readonly string
  public static readonly int BC_STRING_CHUNK = 'R'; // non-readonly string
  
  public static readonly int BC_STRING_DIRECT = 0x00;
  public static readonly int STRING_DIRECT_MAX = 0x1f;
  public static readonly int BC_STRING_SHORT = 0x30;
  public static readonly int STRING_SHORT_MAX = 0x3ff;
  
  public static readonly int BC_TRUE = 'T';

  public static readonly int P_PACKET_CHUNK = 0x4f;
  public static readonly int P_PACKET = 'P';

  public static readonly int P_PACKET_DIRECT = 0x80;
  public static readonly int PACKET_DIRECT_MAX = 0x7f;

  public static readonly int P_PACKET_SHORT = 0x70;
  public static readonly int PACKET_SHORT_MAX = 0xfff;
}

}