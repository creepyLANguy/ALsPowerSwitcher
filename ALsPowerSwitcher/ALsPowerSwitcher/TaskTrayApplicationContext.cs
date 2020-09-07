using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace ALsPowerSwitcher
{
  internal struct Plan
  {
    public string Name;
    public string Guid;
    public bool IsActive;
  }

  public class TaskTrayApplicationContext : ApplicationContext
  {
    private const int BalloonTime = 1500;
    private const string PlanChangedMessage = "Power Plan Changed";

    private readonly NotifyIcon _notifyIcon = new NotifyIcon();

    private List<Plan> _plans = new List<Plan>();

    //private bool _doubleClicked = false;

    private static readonly List<string> Whitelist = new List<string>()
    {
      "AMD Ryzen High Performance",
      "Power saver",
    };
    
    private static readonly List<KeyValuePair<string, string>> Replacements = new List<KeyValuePair<string, string>>()
    {
      new KeyValuePair<string, string>("RyzenT", "Ryzen")
    };

    public TaskTrayApplicationContext()
    {
      RefreshPlans();

      _notifyIcon.Click += NotifyIcon_Click;
      //_notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
      //_notifyIcon.Icon = Properties.Resources.Power_saver;
      _notifyIcon.Visible = true;
    }

    /*
    private void InvokeRightClick()
    {
      var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
      if (mi != null)
      {
        mi.Invoke(_notifyIcon, null);
      }
      else
      {
        Console.WriteLine("Failed at InvokeRightClick()");
      }
    }
    */

    private void NotifyIcon_Click(object sender, EventArgs e)
    {
      /*
      if (_doubleClicked)
      {
        _doubleClicked = false;
        return;
      }
      */

      if (((MouseEventArgs)e).Button == MouseButtons.Left)
      {
        //InvokeRightClick();
        TogglePlan();
      }
    }

    private void TogglePlan()
    {
      var index = -1;
      var items = _notifyIcon.ContextMenu.MenuItems;
      for (var i = 0; i < _plans.Count; ++i)
      {
        if (!items[i].Text.Contains("*"))
        {
          continue;
        }

        index = i + 1;
        if (index >= _plans.Count)
        {
          index = 0;
        }

        break;
      }

      var m = new MenuItem(items[index].Text) {Tag = items[index].Tag};
      SwitchPlan(m, null);
    }

    /*
    private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    {
      //_doubleClicked = true;
      TogglePlan();
    }
    */

    private void RefreshPlans()
    {
      _plans = QueryPlans();

      _notifyIcon.ContextMenu = new ContextMenu();
      foreach (var plan in _plans)
      {
        var m = new MenuItem(plan.Name, SwitchPlan) {Tag = plan.Guid};

        if (plan.IsActive)
        {
          _notifyIcon.Text = plan.Name.Replace("*", "");
          SetIcon(_notifyIcon.Text);
        }

        _notifyIcon.ContextMenu.MenuItems.Add(m);
      }
      _notifyIcon.ContextMenu.MenuItems.Add("-");
      var exitMenuItem = new MenuItem("Exit", Exit);
      _notifyIcon.ContextMenu.MenuItems.Add(exitMenuItem);
    }

    private static List<Plan> QueryPlans()
    {
      var process = new Process
      {
        StartInfo = new ProcessStartInfo()
        {
          UseShellExecute = false,
          CreateNoWindow = true,
          WindowStyle = ProcessWindowStyle.Hidden,
          FileName = "powercfg",
          Arguments = "/l",
          RedirectStandardError = true,
          RedirectStandardOutput = true,
          //StandardOutputEncoding = Encoding.UTF8
        }
      };
      process.Start();

      var s = process.StandardOutput.ReadToEnd().Split('\n');
      process.WaitForExit();

      return ScrapePlansFromQueryResult(s);
    }

    private static List<Plan> ScrapePlansFromQueryResult(string[] results)
    {
      //Just to get the items in some sort of reverse length order - looks better.
      Array.Sort(results, (x, y) => y.Length.CompareTo(x.Length));

      //Just want items that are actual power schemes.
      var filtered = results.Where(item => item.Contains("Power Scheme GUID:")).ToList();

      //Mostly for removing things like the poorly encoded 'TM' from "RyzenTM"
      var replaced = filtered.Select(item => Replacements.Aggregate(item, (current, rep) => current.Replace(rep.Key, rep.Value))).ToList();

      //Only care about specific power plans as specified by the whitelist.
      var whitelisted = (from item in replaced from w in Whitelist where item.Contains(w) select item).ToList();

      var list = new List<Plan>();
      foreach (var item in whitelisted)
      {
        var nameStart = (item.IndexOf('(')) + 1;
        var nameEnd = item.IndexOf(')');
        var name = item.Substring(nameStart, nameEnd - nameStart);
        var isActive = false;
        if (item.Contains("*"))
        {
          name += " *";
          isActive = true;
        }

        var guidStart = (item.IndexOf(':')) + 1;
        var guidEnd = item.IndexOf('(');
        var guid = item.Substring(guidStart, guidEnd - guidStart).Trim();

        var p = new Plan {Name = name, Guid = guid, IsActive = isActive};
        list.Add(p);
      }

      return list;
    }

    private void SwitchPlan(object sender, EventArgs e)
    {
      var guid = ((MenuItem)sender).Tag.ToString();

      var process = new Process
      {
        StartInfo = new ProcessStartInfo()
        {
          UseShellExecute = false,
          CreateNoWindow = true,
          WindowStyle = ProcessWindowStyle.Hidden,
          FileName = "powercfg",
          Arguments = "/s " + guid,
          RedirectStandardError = true,
          RedirectStandardOutput = true
        }
      };
      process.Start();
      process.WaitForExit();

      var text = ((MenuItem)sender).Text.Replace("*", "");

      RefreshPlans();

      SetIcon(text);
      
      _notifyIcon.Visible = true;

      _notifyIcon.BalloonTipTitle = PlanChangedMessage;
      _notifyIcon.BalloonTipText = text;
      _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
      _notifyIcon.ShowBalloonTip(BalloonTime);
    }

    private void SetIcon(string iconName)
    {
      iconName = iconName.Trim();
      var icon = ResourceExtended.GetIconByRawName(iconName) ?? Properties.Resources.Default;
      _notifyIcon.Icon = icon;
    }

    private void Exit(object sender, EventArgs e)
    {
      _notifyIcon.Visible = false;
      Application.Exit();
    }
  }
}

