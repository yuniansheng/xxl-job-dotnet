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
public class CollectionDeserializer : AbstractListDeserializer {
  private Class _type;
  
  public CollectionDeserializer(Class type)
  {
    _type = type;
  }
  
  public Class GetType()
  {
    return _type;
  }
  
  public object ReadList(AbstractHessianInput in, int length)
      {
    Collection list = CreateList();

    in.AddRef(list);

    while (! in.IsEnd())
      list.Add(in.ReadObject());

    in.ReadEnd();

    return list;
  }
  
  public object ReadLengthList(AbstractHessianInput in, int length)
      {
    Collection list = CreateList();

    in.AddRef(list);

    for (; length > 0; length--)
      list.Add(in.ReadObject());

    return list;
  }

  private Collection CreateList()
      {
    Collection list = null;
    
    if (_type == null)
      list = new ArrayList();
    else if (! _type.IsInterface()) {
      try {
        list = (Collection) _type.NewInstance();
      } catch (Exception e) {
      }
    }

    if (list != null) {
    }
    else if (SortedSet.class.IsAssignableFrom(_type))
      list = new TreeSet();
    else if (Set.class.IsAssignableFrom(_type))
      list = new HashSet();
    else if (List.class.IsAssignableFrom(_type))
      list = new ArrayList();
    else if (Collection.class.IsAssignableFrom(_type))
      list = new ArrayList();
    else {
      try {
        list = (Collection) _type.NewInstance();
      } catch (Exception e) {
        throw new IOExceptionWrapper(e);
      }
    }

    return list;
  }
  
  public string ToString()
  {
    return GetClass().GetSimpleName() + "[" + _type + "]";
  }
}



}