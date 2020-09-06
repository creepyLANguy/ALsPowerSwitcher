using System.Drawing;
using ALsPowerSwitcher.Properties;

namespace ALsPowerSwitcher
{
  public class ResourceExtended
  {
    public static Icon GetIconByRawName(string iconName)
    {
      var obj = Resources.ResourceManager.GetObject(iconName, Resources.Culture);
      return (Icon)obj;
    }
  }
}