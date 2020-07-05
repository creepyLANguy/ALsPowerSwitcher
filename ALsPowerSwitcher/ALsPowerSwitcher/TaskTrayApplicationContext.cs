using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace ALsPowerSwitcher
{
  struct Plan
  {
    public string name;
    public string guid;
    public bool isActive;
  }

  public class TaskTrayApplicationContext : ApplicationContext
  {
    private readonly int balloonTime = 1500;
    
    private readonly NotifyIcon notifyIcon = new NotifyIcon();

    private List<Plan> plans = new List<Plan>();

    public TaskTrayApplicationContext()
    {
      RefreshPlans();

      notifyIcon.Click += NotifyIcon_Click;
      notifyIcon.Icon = Properties.Resources.AppIcon;
      notifyIcon.Visible = true;
    }

    private void InvokeRightClick()
    {
      MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                 BindingFlags.Instance | BindingFlags.NonPublic);
      mi.Invoke(notifyIcon, null);
    }

    private void NotifyIcon_Click(object sender,EventArgs e)
    {
      if (((MouseEventArgs)e).Button == MouseButtons.Left)
      {
        InvokeRightClick();
      }     
    }

    void RefreshPlans()
    {
      plans = QueryPlans();

      List<MenuItem> items = new List<MenuItem>();

      notifyIcon.ContextMenu = new ContextMenu();
      foreach (var plan in plans)
      {
        var m = new MenuItem(plan.name, new EventHandler(SwitchPlan));
        m.Tag = plan.guid;

        if (plan.isActive)
        {
          notifyIcon.Text = plan.name.Replace("*", "");
        }

        notifyIcon.ContextMenu.MenuItems.Add(m);
      }
      notifyIcon.ContextMenu.MenuItems.Add("-");
      MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));
      notifyIcon.ContextMenu.MenuItems.Add(exitMenuItem);
    }

    List<Plan> QueryPlans()
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
      string[] s = process.StandardOutput.ReadToEnd().Split('\n');
      process.WaitForExit();

      return ScrapePlansFromQueryResult(s);
    }

    List<Plan> ScrapePlansFromQueryResult(string[] s)
    {
      Array.Sort(s, (x, y) => y.Length.CompareTo(x.Length));
      var l = new List<Plan>();
      foreach (var item in s)
      {
        if (item.Contains("(") == false || item.Contains("(*") == true || item.Contains("Balanced") == true)
        {
          continue;
        }

        if (item.Contains("AMD") || item.Contains("saver"))
        {
          int nameStart = (item.IndexOf('(')) + 1;
          int nameEnd = item.IndexOf(')');
          string name = item.Substring(nameStart, nameEnd - nameStart);
          name = name.Replace("RyzenT", "Ryzen");
          bool isActive = false;
          if (item.Contains("*"))
          {
            name += " *";
            isActive = true;
          }

          int guidStart = (item.IndexOf(':')) + 1;
          int guidEnd = item.IndexOf('(');
          string guid = item.Substring(guidStart, guidEnd - guidStart).Trim();

          var p = new Plan();
          p.name = name;
          p.guid = guid;
          p.isActive = isActive;
          l.Add(p);
        }
      }
      return l;
    }

    void SwitchPlan(object sender, EventArgs e)
    {
      //MessageBox.Show("powercfg /s " + ((MenuItem)sender).Tag);

      string guid = ((MenuItem)sender).Tag.ToString();

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
      notifyIcon.BalloonTipTitle = "Power Plan Changed";
      notifyIcon.BalloonTipText = text;
      notifyIcon.BalloonTipIcon = ToolTipIcon.None;
      notifyIcon.ShowBalloonTip(balloonTime);

      RefreshPlans();
    }

    //private void PerformToggle()
    //{
    //  foreach (var p in plans)
    //  {
    //    if (p.isActive != false)
    //    {
    //      continue;
    //    }

    //    var process = new Process();
    //    process.StartInfo = new ProcessStartInfo()
    //    {
    //      UseShellExecute = false,
    //      CreateNoWindow = true,
    //      WindowStyle = ProcessWindowStyle.Hidden,
    //      FileName = "powercfg",
    //      Arguments = "/s " + p.guid,
    //      RedirectStandardError = true,
    //      RedirectStandardOutput = true
    //    };
    //    process.Start();
    //    process.WaitForExit();

    //    var text = p.name;
    //    notifyIcon.BalloonTipTitle = "Power Plan Changed";
    //    notifyIcon.BalloonTipText = text;
    //    notifyIcon.BalloonTipIcon = ToolTipIcon.None;
    //    notifyIcon.ShowBalloonTip(balloonTime);

    //    RefreshPlans();
    //    break;
    //  }
    //}

    void Exit(object sender, EventArgs e)
    {
      notifyIcon.Visible = false;
      Application.Exit();
    }
  }
}

