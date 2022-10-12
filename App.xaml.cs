using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace VanSwitch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (VistaSecurity.IsAdmin()) 
            {
                MainWindow window = new MainWindow();
                foreach (string s in e.Args)
                {
                    if (s == "-disableac")
                    {
                        window.DisableAC();
                    }
                    if (s == "-enableac")
                    {
                        window.EnableAC();
                    }
                }
            }
        }
    }
}
