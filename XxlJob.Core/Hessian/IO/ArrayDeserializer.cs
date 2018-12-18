using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Deserializing a Java array
/// </summary>
public class ArrayDeserializer : AbstractListDeserializer {
  private Class _componentType;
  private Class _type;
  
  public ArrayDeserializer(Class componentType)
  {
    _componentType = componentType;
    
    if (_componentType != null) {
      try {
        _type = Array.NewInstance(_componentType, 0).GetClass();
      } catch (Exception e) {
      }
    }

    if (_type == null)
      _type = Object[].class;
  }

  public Class GetType()
  {
    return _type;
  }

  /// <summary>
  /// Reads the array.
  /// </summary>
  public object ReadList(AbstractHessianInput in, int length)
      {
    if (length >= 0) {
      object[] data = CreateArray(length);

      in.AddRef(data);
      
      if (_componentType != null) {
        for (int i = 0; i < data.length; i++)
          data[i] = in.ReadObject(_componentType);
      }
      else {
        for (int i = 0; i < data.length; i++)
          data[i] = in.ReadObject();
      }

      in.ReadListEnd();

      return data;
    }
    else {
      ArrayList list = new ArrayList();

      in.AddRef(list);

      if (_componentType != null) {
        while (! in.IsEnd())
          list.Add(in.ReadObject(_componentType));
      }
      else {
        while (! in.IsEnd())
          list.Add(in.ReadObject());
      }

      in.ReadListEnd();

      object[] data = CreateArray(list.Size());
      for (int i = 0; i < data.length; i++)
        data[i] = list.Get(i);

      return data;
    }
  }

  /// <summary>
  /// Reads the array.
  /// </summary>
  public object ReadLengthList(AbstractHessianInput in, int length)
      {
    object[] data = CreateArray(length);

    in.AddRef(data);
      
    if (_componentType != null) {
      for (int i = 0; i < data.length; i++)
        data[i] = in.ReadObject(_componentType);
    }
    else {
      for (int i = 0; i < data.length; i++)
        data[i] = in.ReadObject();
    }

    return data;
  }

  protected object[] CreateArray(int length)
  {
    if (_componentType != null)
      return (object[] ) Array.NewInstance(_componentType, length);
    else
      return new Object[length];
  }

  public string ToString()
  {
    return "ArrayDeserializer[" + _componentType + "]";
  }
}

}