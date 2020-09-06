using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

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

    private readonly NotifyIcon _notifyIcon = new NotifyIcon();

    private List<Plan> _plans = new List<Plan>();
    
    //private bool _doubleClicked = false;

    public TaskTrayApplicationContext()
    {
      RefreshPlans();

      _notifyIcon.Click += NotifyIcon_Click;
      //_notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
      //_notifyIcon.Icon = Properties.Resources.Power_saver;
      _notifyIcon.Visible = true;
    }

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

      var m = new MenuItem(items[index].Text);
      m.Tag = items[index].Tag;
      SwitchPlan(m, null);
    }

    private void NotifyIcon_DoubleClick(object sender, EventArgs e)
    {
      //_doubleClicked = true;
      TogglePlan();
    }

    private void RefreshPlans()
    {
      _plans = QueryPlans();

      var items = new List<MenuItem>();

      _notifyIcon.ContextMenu = new ContextMenu();
      foreach (var plan in _plans)
      {
        var m = new MenuItem(plan.Name, SwitchPlan);
        m.Tag = plan.Guid;

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
      var process = new Process();
      process.StartInfo = new ProcessStartInfo()
      {
        UseShellExecute = false,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        FileName = "powercfg",
        Arguments = "/l",
        RedirectStandardError = true,
        RedirectStandardOutput = true
      };
      process.Start();

      var s = process.StandardOutput.ReadToEnd().Split('\n');
      process.WaitForExit();

      return ScrapePlansFromQueryResult(s);
    }

    private static List<Plan> ScrapePlansFromQueryResult(string[] s)
    {
      Array.Sort(s, (x, y) => y.Length.CompareTo(x.Length));
      var l = new List<Plan>();
      foreach (var item in s)
      {
        if (item.Contains("(") == false || item.Contains("(*") || item.Contains("Balanced"))
        {
          continue;
        }

        if (!item.Contains("AMD") && !item.Contains("saver"))
        {
          continue;
        }

        var nameStart = (item.IndexOf('(')) + 1;
        var nameEnd = item.IndexOf(')');
        var name = item.Substring(nameStart, nameEnd - nameStart);
        name = name.Replace("RyzenT", "Ryzen");
        var isActive = false;
        if (item.Contains("*"))
        {
          name += " *";
          isActive = true;
        }

        var guidStart = (item.IndexOf(':')) + 1;
        var guidEnd = item.IndexOf('(');
        var guid = item.Substring(guidStart, guidEnd - guidStart).Trim();

        var p = new Plan();
        p.Name = name;
        p.Guid = guid;
        p.IsActive = isActive;
        l.Add(p);
      }
      return l;
    }

    private void SwitchPlan(object sender, EventArgs e)
    {
      var guid = ((MenuItem)sender).Tag.ToString();

      var process = new Process();
      process.StartInfo = new ProcessStartInfo()
      {
        UseShellExecute = false,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        FileName = "powercfg",
        Arguments = "/s " + guid,
        RedirectStandardError = true,
        RedirectStandardOutput = true
      };
      process.Start();
      process.WaitForExit();

      var text = ((MenuItem)sender).Text.Replace("*", "");

      RefreshPlans();

      SetIcon(text);

      _notifyIcon.BalloonTipTitle = "Power Plan Changed";
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

