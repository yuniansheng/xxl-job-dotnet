using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian.IO
{





/// <summary>
/// Loads a class from the classloader.
/// </summary>
public class ClassFactory
{
  private static ArrayList<Allow> _staticAllowList;
  
  private ClassLoader _loader;
  private bool _isWhitelist;
  
  private ArrayList<Allow> _allowList;
  
  ClassFactory(ClassLoader loader)
  {
    _loader = loader;
  }
  
  public Type Load(string className)
  {
    if (IsAllow(className)) {
      return Class.ForName(className, false, _loader);
    }
    else {
      return HashMap.class;
    }
  }
  
  private bool IsAllow(string className)
  {
    ArrayList<Allow> allowList = _allowList;
    
    if (allowList == null) {
      return true;
    }
    
    int size = allowList.Size();
    for (int i = 0; i < size; i++) {
      Allow allow = allowList.Get(i);
      
      Boolean isAllow = allow.Allow(className);
      
      if (isAllow != null) {
        return isAllow;
      }
    }
    
    return false;
  }
  
  public void SetWhitelist(bool isWhitelist)
  {
    _isWhitelist = isWhitelist;
    
    InitAllow();
  }
  
  public void Allow(string pattern)
  {
    InitAllow();
    
    synchronized (this) {
      _allowList.Add(new Allow(ToPattern(pattern), true));
    }
  }
  
  public void Deny(string pattern)
  {
    InitAllow();
    
    synchronized (this) {
      _allowList.Add(new Allow(ToPattern(pattern), false));
    }
  }
  
  private string ToPattern(string pattern)
  {
    pattern = pattern.Replace(".", "\\.");
    pattern = pattern.Replace("*", ".*");
    
    return pattern;
  }
  
  private void InitAllow()
  {
    synchronized (this) {
      if (_allowList == null) {
        _allowList = new ArrayList<Allow>();
        _allowList.AddAll(_staticAllowList);
      }
    }
  }
  
  static class Allow {
    private Boolean _isAllow;
    private Pattern _pattern;
    
    private Allow(string pattern, bool isAllow)
    {
      _isAllow = isAllow;
      _pattern = Pattern.Compile(pattern);
    }
    
    Boolean Allow(string className)
    {
      if (_pattern.Matcher(className).Matches()) {
        return _isAllow;
      }
      else {
        return null;
      }
    }
  }
  
  static {
    _staticAllowList = new ArrayList<Allow>();
    
    _staticAllowList.Add(new Allow("java\\..+", true));
  }
}

}