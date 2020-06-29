using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace ALsPowerSwitcher
{
    public class TaskTrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon notifyIcon = new NotifyIcon();

        public TaskTrayApplicationContext()
        {
            RefreshMenuItems();

            notifyIcon.Icon = Properties.Resources.AppIcon;
            notifyIcon.Visible = true;
        }

        void RefreshMenuItems()
        {
            notifyIcon.ContextMenu = new ContextMenu();
            foreach (MenuItem plan in GetPowerPlans())
            {
                notifyIcon.ContextMenu.MenuItems.Add(plan);
            }
            notifyIcon.ContextMenu.MenuItems.Add("-");
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));
            notifyIcon.ContextMenu.MenuItems.Add(exitMenuItem);
        }

        void SwitchPlan()
        {
            //AL. 
            //TODO
            MessageBox.Show("powercfg /s xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
            RefreshMenuItems();
        }

        void Exit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private MenuItem[] GetPowerPlans()
        {
            Process process = new Process();
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

            s = SortAndFilterStrings(s);

            var menuItems = new List<MenuItem>();
            foreach (var item in s)
            {
                if (item.Contains("(") == false || item.Contains("(*") == true)
                {
                    continue;
                }

                if (item.Contains("AMD") || item.Contains("saver"))
                {
                    int start = (item.IndexOf('(')) + 1;
                    int end = item.IndexOf(')');
                    string sub = item.Substring(start, end - start);
                    sub = sub.Replace("RyzenT", "Ryzen");
                    if (item.Contains("*"))
                    {
                        sub += " *";
                    }
                    menuItems.Add(new MenuItem(sub));
                }
            }
            return menuItems.ToArray();
        }

        string[] SortAndFilterStrings(string[] s)
        {
            Array.Sort(s, (x, y) => y.Length.CompareTo(x.Length));
            List<string> l = new List<string>();
            foreach (var item in s)
            {
                if (item.Contains("(") == false || item.Contains("(*") == true)
                {
                    continue;
                }

                if (item.Contains("AMD") || item.Contains("saver"))
                {
                    l.Add(item);
                }
            }
            return l.ToArray();
        }
    }
}
