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
using System.Net;
using System.Net.Http;
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

namespace VanSwitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string VER = "1.0.5";
        const double POLLING_DELAY = 5;
        readonly NotifyIconWrapper notifyicon = new NotifyIconWrapper();
        readonly ContextMenu cm = new ContextMenu();
        //bool opened = false;
        readonly MenuItem autodisable = new MenuItem();
        readonly MenuItem startUpCheck = new MenuItem();
        //NativeMethods.WinEventDelegate dele = null;
        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            IntPtr handle = IntPtr.Zero;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = NativeMethods.GetForegroundWindow();

            if (NativeMethods.GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
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
        void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (Properties.Settings.Default.autodisable && ACEnabled())
                {
                    ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                    string processName = obj["Name"].ToString();
                    //Debug.WriteLine($"VanSwitch {VER}: Process stopped. {processName} - {obj["Handle"]}");
                    if (processName == "VALORANT.exe")
                    {
                        Debug.WriteLine($"VanSwitch {VER} : " + "Valorant stopped");
                        DisableAC();
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"VanSwitch {VER} : " + ex.StackTrace); }
        }
        public MainWindow()
        {
            KillDuplicates();
            try
            {
                MenuItem abouttext = new MenuItem();
                BitmapSource shield = UACShield();
                abouttext.Header = $"VanSwitch ({VER})";
                abouttext.IsEnabled = false;
                abouttext.Icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri($"pack://application:,,,/Icons/enabled.ico"))
                    {
                        DecodePixelHeight = 16,
                        DecodePixelWidth = 16
                    }
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
                MenuItem checkForUpdates = new MenuItem
                {
                    Header = "Check for updates"
                };
                checkForUpdates.Click += CheckForUpdates_Click; ;
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
                cm.Items.Add(checkForUpdates);
                cm.Items.Add(exit);
                cm.StaysOpen = false;
                //dele = new NativeMethods.WinEventDelegate(WinEventProc);
                //IntPtr m_hhook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
                string queryString = $"SELECT * FROM __InstanceDeletionEvent WITHIN {POLLING_DELAY} WHERE TargetInstance ISA 'Win32_Process'";
                ManagementEventWatcher managementEventWatcher = new ManagementEventWatcher(queryString);
                managementEventWatcher.EventArrived += processStopEvent_EventArrived;
                managementEventWatcher.Start();
                GC.Collect();
            }
            catch ( Exception ex){ Debug.WriteLine($"VanSwitch {VER} : "+ex);  }
            if (!DoesServiceExist("vgk") || !DoesServiceExist("vgc"))
            {
                MessageBox.Show("Couldn't find Vanguard Services on this computer make sure it is installed correctly (If you are certain it is installed correctly try to run this app as administrator)", "Vanguard Isn't Found", MessageBoxButton.OK, MessageBoxImage.Error);
                Process.GetCurrentProcess().Kill();
            }
            Top = -1000;
            Left = -1000;
            InitializeComponent();
        }
        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        void CheckForUpdates()
        {
            try
            {
                System.Net.WebClient client = new System.Net.WebClient() { Encoding = Encoding.UTF8 };
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                Debug.WriteLine($"VanSwitch {VER} : " + "Checking for updates");
                string latestVersion = client.DownloadString("https://raw.githubusercontent.com/MurkyYT/VanSwitch/master/version.txt");
                Debug.WriteLine($"VanSwitch {VER} : " + $"The latest version is {latestVersion}");
                if (latestVersion == VER)
                {
                    Debug.WriteLine($"VanSwitch {VER} : " + "Latest version installed");
                    MessageBox.Show("You have the latest version!", "Check for updates (VanSwitch)", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine($"VanSwitch {VER} : " + $"Newer version found {VER} --> {latestVersion}");
                    MessageBoxResult result = MessageBox.Show($"Found newer verison ({latestVersion}) would you like to downlaod it?", "Check for updates (VanSwitch)", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        Debug.WriteLine($"VanSwitch {VER} : " + "Downloading latest version");
                        System.Diagnostics.Process.Start("https://github.com/MurkyYT/VanSwitch/releases/latest/download/VanSwitch.exe");
                    }
                }
            }
            catch 
            {
                MessageBox.Show("Couldn't check for updates", "Check for updates (VanSwitch)", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            this.notifyicon.Icon = Properties.Resources.disabled;
            this.notifyicon.Tip = "Vanguard Disabled";
            this.notifyicon.Update();
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
        void UpdateNotifyIcon()
        {
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
        public bool ACEnabled()
        {
            if (ServiceHelper.CheckSerivce("vgk") == "Running")
            {
                return true;
            }
            return false;
        }
        public bool EnableAC()
        {
            try
            {
                Debug.WriteLine($"VanSwitch {VER} : " + "Enabling vanguard...");
                RegistryHelper.SetRegistryValue("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Riot Vanguard", Properties.Settings.Default.vgtrayLocation, RegistryHive.LocalMachine, registryView: RegistryView.Registry64);
                var vgksc = new ServiceController("vgk");
                ServiceHelper.ChangeStartMode(vgksc, ServiceStartMode.System);
                Debug.WriteLine($"VanSwitch {VER} : " + "Enabled vgk service");
                var vgcsc = new ServiceController("vgc");
                ServiceHelper.ChangeStartMode(vgcsc, ServiceStartMode.Manual);
                Debug.WriteLine($"VanSwitch {VER} : " + "Enabled vgc service");
                //System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VanSwitch {VER} : " + ex.StackTrace);
                return false; 
            }
        }
        public bool DisableAC()
        {
            try
            {
                Debug.WriteLine($"VanSwitch {VER} : " + "Disabling vanguard...");
                var vgksc = new ServiceController("vgk");
                var vgcsc = new ServiceController("vgc");
                if (vgcsc.Status == ServiceControllerStatus.Running || vgcsc.Status == ServiceControllerStatus.StartPending || vgcsc.Status == ServiceControllerStatus.ContinuePending)
                {
                    vgcsc.Stop();
                    Debug.WriteLine($"VanSwitch {VER} : " + "Stopped vgc");
                }
                if (vgksc.Status == ServiceControllerStatus.Running || vgksc.Status == ServiceControllerStatus.StartPending || vgksc.Status == ServiceControllerStatus.ContinuePending)
                {
                    vgksc.Stop();
                    Debug.WriteLine($"VanSwitch {VER} : " + "Stopped vgk");
                }
                foreach (var process in Process.GetProcessesByName("vgtray")) 
                { 
                    process.Kill();
                    Debug.WriteLine($"VanSwitch {VER} : " + "Killed vgtray");
                }
                RegistryHelper.RemoveRegistryValue("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Riot Vanguard", RegistryHive.LocalMachine, registryView: RegistryView.Registry64);
                this.notifyicon.Icon = Properties.Resources.disabled;
                this.notifyicon.Tip = "Vanguard Disabled";
                this.notifyicon.Update();
                ServiceHelper.ChangeStartMode(vgksc, ServiceStartMode.Disabled);
                Debug.WriteLine($"VanSwitch {VER} : " + "Disabled vgk service");
                ServiceHelper.ChangeStartMode(vgcsc, ServiceStartMode.Disabled);
                Debug.WriteLine($"VanSwitch {VER} : " + "Disabled vgc service");
                return true;
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"VanSwitch {VER} : " + ex.StackTrace);
                return false; 
            }
        }
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Visibility = Visibility.Hidden;
            this.notifyicon.ShowTip = true;
            this.notifyicon.RightMouseButtonClick += Notifyicon_RightMouseButtonClick;
            UpdateNotifyIcon();
            if (Properties.Settings.Default.vgtrayLocation != RegistryHelper.GetRegistryValue("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Riot Vanguard", RegistryHive.LocalMachine, RegistryView.Registry64) && RegistryHelper.GetRegistryValue("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Riot Vanguard", RegistryHive.LocalMachine, RegistryView.Registry64) != "")
            {
                Properties.Settings.Default.vgtrayLocation = RegistryHelper.GetRegistryValue("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\Riot Vanguard", RegistryHive.LocalMachine, RegistryView.Registry64);
                Properties.Settings.Default.Save();
            }
        }
        private void Notifyicon_RightMouseButtonClick(object sender, MouseLocationEventArgs e)
        {
            cm.IsOpen = true;
            Activate();
        }
    }
}
