using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Factory for creating HessianInput and HessianOutput streams.
/// </summary>
public class HessianFactory
{
  public static readonly Logger log
    = Logger.GetLogger(HessianFactory.class.GetName());

  private SerializerFactory _serializerFactory;
  private SerializerFactory _defaultSerializerFactory;

  private readonly HessianFreeList<Hessian2Output> _freeHessian2Output
    = new HessianFreeList<Hessian2Output>(32);

  private readonly HessianFreeList<HessianOutput> _freeHessianOutput
    = new HessianFreeList<HessianOutput>(32);

  private readonly HessianFreeList<Hessian2Input> _freeHessian2Input
    = new HessianFreeList<Hessian2Input>(32);

  private readonly HessianFreeList<HessianInput> _freeHessianInput
    = new HessianFreeList<HessianInput>(32);

  public HessianFactory()
  {
    _defaultSerializerFactory = SerializerFactory.CreateDefault();
    _serializerFactory = _defaultSerializerFactory;
  }

  public void SetSerializerFactory(SerializerFactory factory)
  {
    _serializerFactory = factory;
  }

  public SerializerFactory GetSerializerFactory()
  {
    // the default serializer factory cannot be modified by external
    // callers
    if (_serializerFactory == _defaultSerializerFactory) {
      _serializerFactory = new SerializerFactory();
    }

    return _serializerFactory;
  }
  
  public void SetWhitelist(bool isWhitelist)
  {
    GetSerializerFactory().GetClassFactory().SetWhitelist(isWhitelist);
  }
  
  public void Allow(string pattern)
  {
    GetSerializerFactory().GetClassFactory().Allow(pattern);
  }
  
  public void Deny(string pattern)
  {
    GetSerializerFactory().GetClassFactory().Deny(pattern);
  }

  /// <summary>
  /// Creates a new Hessian 2.0 deserializer.
  /// </summary>
  public Hessian2Input CreateHessian2Input(InputStream is)
  {
    Hessian2Input in = _freeHessian2Input.Allocate();
    
    if (in == null) {
      in = new Hessian2Input(is);
      in.SetSerializerFactory(GetSerializerFactory());
    }
    else {
      in.Init(is);
    }

    return in;
  }

  /// <summary>
  /// Frees a Hessian 2.0 deserializer
  /// </summary>
  public void FreeHessian2Input(Hessian2Input in)
  {
    if (in == null)
      return;

    in.Free();

    _freeHessian2Input.Free(in);
  }

  /// <summary>
  /// Creates a new Hessian 2.0 deserializer.
  /// </summary>
  public Hessian2StreamingInput CreateHessian2StreamingInput(InputStream is)
  {
    Hessian2StreamingInput in = new Hessian2StreamingInput(is);
    in.SetSerializerFactory(GetSerializerFactory());

    return in;
  }

  /// <summary>
  /// Frees a Hessian 2.0 deserializer
  /// </summary>
  public void FreeHessian2StreamingInput(Hessian2StreamingInput in)
  {
  }

  /// <summary>
  /// Creates a new Hessian 1.0 deserializer.
  /// </summary>
  public HessianInput CreateHessianInput(InputStream is)
  {
    return new HessianInput(is);
  }

  /// <summary>
  /// Creates a new Hessian 2.0 serializer.
  /// </summary>
  public Hessian2Output CreateHessian2Output(OutputStream os)
  {
    Hessian2Output out = CreateHessian2Output();
    
    out.Init(os);
    
    return out;
  }

  /// <summary>
  /// Creates a new Hessian 2.0 serializer.
  /// </summary>
  public Hessian2Output CreateHessian2Output()
  {
    Hessian2Output out = _freeHessian2Output.Allocate();

    if (out == null) {
      out = new Hessian2Output();

      out.SetSerializerFactory(GetSerializerFactory());
    }

    return out;
  }

  /// <summary>
  /// Frees a Hessian 2.0 serializer
  /// </summary>
  public void FreeHessian2Output(Hessian2Output out)
  {
    if (out == null)
      return;

    out.Free();

    _freeHessian2Output.Free(out);
  }

  /// <summary>
  /// Creates a new Hessian 2.0 serializer.
  /// </summary>
  public Hessian2StreamingOutput CreateHessian2StreamingOutput(OutputStream os)
  {
    Hessian2Output out = CreateHessian2Output(os);

    return new Hessian2StreamingOutput(out);
  }

  /// <summary>
  /// Frees a Hessian 2.0 serializer
  /// </summary>
  public void FreeHessian2StreamingOutput(Hessian2StreamingOutput out)
  {
    if (out == null)
      return;

    FreeHessian2Output(out.GetHessian2Output());
  }

  /// <summary>
  /// Creates a new Hessian 1.0 serializer.
  /// </summary>
  public HessianOutput CreateHessianOutput(OutputStream os)
  {
    return new HessianOutput(os);
  }

  public OutputStream CreateHessian2DebugOutput(OutputStream os,
                                                Logger log,
                                                Level level)
  {
    HessianDebugOutputStream out
      = new HessianDebugOutputStream(os, log, level);

    out.StartTop2();

    return out;
  }
}

}