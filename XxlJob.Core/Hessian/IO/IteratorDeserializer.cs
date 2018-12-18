using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{




/// <summary>
/// Deserializing a JDK 1.2 Collection.
/// </summary>
public class IteratorDeserializer : AbstractListDeserializer {
  private static IteratorDeserializer _deserializer;

  public static IteratorDeserializer Create()
  {
    if (_deserializer == null)
      _deserializer = new IteratorDeserializer();

    return _deserializer;
  }
  
    public override object ReadList(AbstractHessianInput in, int length)
      {
    ArrayList list = new ArrayList();

    in.AddRef(list);

    while (! in.IsEnd())
      list.Add(in.ReadObject());

    in.ReadEnd();

    return list.Iterator();
  }
}



}