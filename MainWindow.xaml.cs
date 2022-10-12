using Microsoft.Win32;
using NotifyIconLibrary;
using NotifyIconLibrary.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VanSwitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly NotifyIconWrapper notifyicon = new NotifyIconWrapper();
        readonly ContextMenu cm = new ContextMenu();
        readonly System.Windows.Threading.DispatcherTimer appTimer = new System.Windows.Threading.DispatcherTimer();
        bool found, open = false;
        readonly MenuItem autodisable = new MenuItem();
        readonly MenuItem startUpCheck = new MenuItem();
        private BitmapSource UACShield()
        {
            BitmapSource shieldSource;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                NativeMethods.SHSTOCKICONINFO sii = new NativeMethods.SHSTOCKICONINFO
                {
                    cbSize = (UInt32)Marshal.SizeOf(typeof(NativeMethods.SHSTOCKICONINFO))
                };

                Marshal.ThrowExceptionForHR(NativeMethods.SHGetStockIconInfo(NativeMethods.SHSTOCKICONID.SIID_SHIELD,
                    NativeMethods.SHGSI.SHGSI_ICON | NativeMethods.SHGSI.SHGSI_SMALLICON,
                    ref sii));

                shieldSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    sii.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                NativeMethods.DestroyIcon(sii.hIcon);
            }
            else
            {
                shieldSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    System.Drawing.SystemIcons.Shield.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            return shieldSource;
        }
        public MainWindow()
        {
            try
            {
                MenuItem abouttext = new MenuItem();
                BitmapSource shield = UACShield();
                abouttext.Header = $"VanSwitch ({new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTimeUtc:dd/MM/yyyy - HH:mm:ss})";
                abouttext.IsEnabled = false;
                abouttext.Icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri($"pack://application:,,,/Icons/enabled.ico"))
                };
                MenuItem disable = new MenuItem
                {
                    Header = "Disable Vanguard"
                };
                disable.Click += Disable_Click;
                disable.Icon = new System.Windows.Controls.Image
                {
                    Source = shield
                };
                MenuItem enable = new MenuItem
                {
                    Header = "Enable Vanguard"
                };
                enable.Click += Enable_Click;
                enable.Icon = new System.Windows.Controls.Image
                {
                    Source = shield
                };
                autodisable.IsChecked = Properties.Settings.Default.autodisable;
                autodisable.Header = "Exit Vanguard Automatically";
                autodisable.IsCheckable = true;
                autodisable.Click += Autodisable_Click;
                startUpCheck.IsChecked = Properties.Settings.Default.runAtStartUp;
                startUpCheck.Header = "Start With Windows";
                startUpCheck.IsCheckable = true;
                startUpCheck.Click += StartUpCheck_Click;
                MenuItem exit = new MenuItem
                {
                    Header = "Exit"
                };
                exit.Click += Exit_Click;
                cm.Items.Add(abouttext);
                cm.Items.Add(new Separator());
                cm.Items.Add(enable);
                cm.Items.Add(disable);
                cm.Items.Add(new Separator());
                cm.Items.Add(autodisable);
                cm.Items.Add(startUpCheck);
                cm.Items.Add(new Separator());
                cm.Items.Add(exit);
                cm.StaysOpen = false;
                KillDuplicates();
                GC.Collect();
            }
            catch ( Exception ex){ Debug.WriteLine(ex);  }
            if (!DoesServiceExist("vgk") || !DoesServiceExist("vgc"))
            {
                MessageBox.Show("Couldn't find Vanguard Services on this computer make sure it is installed correctly (If you are certain it is installed correctly try to run this app as administrator)", "Vanguard Isn't Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
            Top = -1000;
            Left = -1000;
            InitializeComponent();
        }
        bool DoesServiceExist(string serviceName)
        {
            if (ServiceController.GetServices().Any(serviceController => serviceController.ServiceName.Equals(serviceName)))
            {
                return true;
            }
            if (ServiceController.GetDevices().Any(serviceController => serviceController.ServiceName.Equals(serviceName)))
            {
                return true;
            }
            return false;
        }
        private void StartUpCheck_Click(object sender, RoutedEventArgs e)
        {
            string appname = Assembly.GetEntryAssembly().GetName().Name;
            string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if ((bool)startUpCheck.IsChecked)
                {
                    Properties.Settings.Default.runAtStartUp = true;
                    rk.SetValue(appname, executablePath);
                }
                else
                {
                    Properties.Settings.Default.runAtStartUp = false;
                    rk.DeleteValue(appname, false);
                }
            }
            Properties.Settings.Default.Save();
        }

        private void Autodisable_Click(object sender, RoutedEventArgs e)
        {
            if (autodisable.IsChecked)
            {
                Properties.Settings.Default.autodisable = true;
            }
            else
            {
                Properties.Settings.Default.autodisable = false;
            }
            Properties.Settings.Default.Save();
        }

        private void Enable_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =
                    MessageBox.Show("Are you sure you want to enable Vanguard? (Your computer will be restarted)", "Enable Vanguard", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (VistaSecurity.IsAdmin() && result == MessageBoxResult.Yes)
            {
                EnableAC();
            }
            else if (result == MessageBoxResult.Yes)
                VistaSecurity.RestartElevated("-enableac");
        }

        private void Disable_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show("Are you sure you want to disable Vanguard?", "Disable Vanguard", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (VistaSecurity.IsAdmin() && result == MessageBoxResult.Yes)
            {
                DisableAC();
            }
            else if (result == MessageBoxResult.Yes)
                VistaSecurity.RestartElevated("-disableac");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void KillDuplicates()
        {
            var currentProcess = Process.GetCurrentProcess();
            var duplicates = Process.GetProcessesByName(currentProcess.ProcessName).Where(o => o.Id != currentProcess.Id);
            if (duplicates.Count() > 0)
            {
                notifyicon.Close();
                App.Current.Shutdown();
            }
        }
        public string CheckSerivce(string SERVICENAME)
        {
            ServiceController sc = new ServiceController(SERVICENAME);
            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    return "Running";
                case ServiceControllerStatus.Stopped:
                    return "Stopped";
                case ServiceControllerStatus.Paused:
                    return "Paused";
                case ServiceControllerStatus.StopPending:
                    return "Stopping";
                case ServiceControllerStatus.StartPending:
                    return "Starting";
                default:
                    return "Status Changing";
            }
        }
        public bool ACEnabled()
        {
            if (CheckSerivce("vgk") == "Running")
            {
                return true;
            }
            return false;
        }
        public bool EnableAC()
        {
            try
            {
                var vgksc = new ServiceController("vgk");
                ServiceHelper.ChangeStartMode(vgksc, ServiceStartMode.System);
                var vgcsc = new ServiceController("vgc");
                ServiceHelper.ChangeStartMode(vgcsc, ServiceStartMode.Manual);
                System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
                return true;
            }
            catch { return false; }
        }
        public bool DisableAC()
        {
            try
            {
                var vgksc = new ServiceController("vgk");
                ServiceHelper.ChangeStartMode(vgksc, ServiceStartMode.Disabled);
                var vgcsc = new ServiceController("vgc");
                ServiceHelper.ChangeStartMode(vgcsc, ServiceStartMode.Disabled);
                if (vgcsc.Status == ServiceControllerStatus.Running || vgcsc.Status == ServiceControllerStatus.StartPending || vgcsc.Status == ServiceControllerStatus.ContinuePending)
                    vgcsc.Stop();
                if (vgksc.Status == ServiceControllerStatus.Running || vgksc.Status == ServiceControllerStatus.StartPending || vgksc.Status == ServiceControllerStatus.ContinuePending)
                    vgksc.Stop();
                foreach (var process in Process.GetProcessesByName("vgtray")) { process.Kill(); }
                return true;
            }
            catch { return false; }
        }
        private void TimerCallback(object sender, EventArgs e)
        {
            try
            {
                if (Properties.Settings.Default.autodisable && ACEnabled())
                {
                    if (Process.GetProcessesByName("Valorant").Length > 0) { found = open = true; }
                    // if the game is closed disables Vanguard
                    if (!found && open)
                    {
                        DisableAC();
                        open = false;
                    }
                    found = false;
                }
                if (ACEnabled())
                {
                    this.notifyicon.Icon = Properties.Resources.enabled;
                    this.notifyicon.Tip = "Vanguard Enabled";
                }
                else
                {
                    this.notifyicon.Icon = Properties.Resources.disabled;
                    this.notifyicon.Tip = "Vanguard Disabled";
                    foreach (var process in Process.GetProcessesByName("vgtray")) { process.Kill(); }
                }
                this.notifyicon.Update();
            }
            catch { }
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Visibility = Visibility.Hidden;
            this.notifyicon.ShowTip = true;
            this.notifyicon.RightMouseButtonClick += Notifyicon_RightMouseButtonClick;
            appTimer.Interval = TimeSpan.FromMilliseconds(1000);
            appTimer.Tick += new EventHandler(TimerCallback);
            appTimer.Start();
        }
        private void Notifyicon_RightMouseButtonClick(object sender, MouseLocationEventArgs e)
        {
            cm.IsOpen = true;
            Activate();
        }
    }
}
