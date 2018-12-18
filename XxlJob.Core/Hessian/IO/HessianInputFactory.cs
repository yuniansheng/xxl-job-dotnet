using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




public class HessianInputFactory
{
  public static readonly Logger log
    = Logger.GetLogger(HessianInputFactory.class.GetName());

  private HessianFactory _factory = new HessianFactory();

  public void SetSerializerFactory(SerializerFactory factory)
  {
    _factory.SetSerializerFactory(factory);
  }

  public SerializerFactory GetSerializerFactory()
  {
    return _factory.GetSerializerFactory();
  }

  public HeaderType ReadHeader(InputStream is)
      {
    int code = is.Read();

    int major = is.Read();
    int minor = is.Read();

    switch (code) {
    case -1:
      throw new IOException("Unexpected end of file for Hessian message");
      
    case 'c':
      if (major >= 2)
        return HeaderType.CALL_1_REPLY_2;
      else
        return HeaderType.CALL_1_REPLY_1;
    case 'r':
      return HeaderType.REPLY_1;
      
    case 'H':
      return HeaderType.HESSIAN_2;

    default:
      throw new IOException((char) code + " 0x" + Integer.ToHexString(code) + " is an unknown Hessian message code.");
    }
  }

  public AbstractHessianInput Open(InputStream is)
      {
    int code = is.Read();

    int major = is.Read();
    int minor = is.Read();

    switch (code) {
    case 'c':
    case 'C':
    case 'r':
    case 'R':
      if (major >= 2) {
        return _factory.CreateHessian2Input(is);
      }
      else {
        return _factory.CreateHessianInput(is);
      }

    default:
      throw new IOException((char) code + " is an unknown Hessian message code.");
    }
  }

  public enum HeaderType {
    CALL_1_REPLY_1,
      CALL_1_REPLY_2,
      HESSIAN_2,
      REPLY_1,
      REPLY_2;

    public bool IsCall1()
    {
      switch (this) {
      case CALL_1_REPLY_1:
      case CALL_1_REPLY_2:
        return true;
      default:
        return false;
      }
    }

    public bool IsCall2()
    {
      switch (this) {
      case HESSIAN_2:
        return true;
      default:
        return false;
      }
    }

    public bool IsReply1()
    {
      switch (this) {
      case CALL_1_REPLY_1:
        return true;
      default:
        return false;
      }
    }

    public bool IsReply2()
    {
      switch (this) {
      case CALL_1_REPLY_2:
      case HESSIAN_2:
        return true;
      default:
        return false;
      }
    }
  }
}

}