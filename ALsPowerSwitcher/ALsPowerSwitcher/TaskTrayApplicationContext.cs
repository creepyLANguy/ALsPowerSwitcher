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
  }

  public class TaskTrayApplicationContext : ApplicationContext
  {
    private readonly int balloonTime = 1500;
    private static readonly string soundPath = @"c:\Windows\Media\Windows Balloon.wav";
    System.Media.SoundPlayer player = new System.Media.SoundPlayer(soundPath);

    private readonly NotifyIcon notifyIcon = new NotifyIcon();

    public TaskTrayApplicationContext()
    {
      RefreshPlans();

      notifyIcon.Click += NotifyIcon_Click;
      notifyIcon.Icon = Properties.Resources.AppIcon;
      notifyIcon.Visible = true;
    }

    private void NotifyIcon_Click(object sender,EventArgs e)
    {
      if (((MouseEventArgs)e).Button == MouseButtons.Left)
      {
        MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                 BindingFlags.Instance | BindingFlags.NonPublic);
        mi.Invoke(notifyIcon, null);
      }
    }

    void RefreshPlans()
    {
      var plans = QueryPlans();

      List<MenuItem> items = new List<MenuItem>();

      notifyIcon.ContextMenu = new ContextMenu();
      foreach (var plan in plans)
      {
        var m = new MenuItem(plan.name, new EventHandler(SwitchPlan));
        m.Tag = plan.guid;

        if (plan.name.Contains("*"))
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
        if (item.Contains("(") == false || item.Contains("(*") == true)
        {
          continue;
        }

        if (item.Contains("AMD") || item.Contains("saver"))
        {
          int nameStart = (item.IndexOf('(')) + 1;
          int nameEnd = item.IndexOf(')');
          string name = item.Substring(nameStart, nameEnd - nameStart);
          name = name.Replace("RyzenT", "Ryzen");
          if (item.Contains("*"))
          {
            name += " *";
          }

          int guidStart = (item.IndexOf(':')) + 1;
          int guidEnd = item.IndexOf('(');
          string guid = item.Substring(guidStart, guidEnd - guidStart).Trim();

          var p = new Plan();
          p.name = name;
          p.guid = guid;
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

      PlaySound();

      RefreshPlans();
    }

    void Exit(object sender, EventArgs e)
    {
      notifyIcon.Visible = false;
      Application.Exit();
    }

    void PlaySound()
    {
      try
      {
        player.Play();
      }
      catch (Exception)
      {        
      }
      
    }

  }
}

