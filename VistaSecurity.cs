using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace VanSwitch
{
    internal class VistaSecurity
    {
        [DllImport("user32")]
        public static extern UInt32 SendMessage
    (IntPtr hWnd, UInt32 msg, UInt32 wParam, UInt32 lParam);

        internal const int BCM_FIRST = 0x1600; //Normal button
        internal const int BCM_SETSHIELD = (BCM_FIRST + 0x000C); //Elevated button
        static internal bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            Debug.WriteLine($"VanSwitch (VistaSecurity) : IsAdmin? = {p.IsInRole(WindowsBuiltInRole.Administrator)}");
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }
        internal static void RestartElevated(string args)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Verb = "runas";
            startInfo.Arguments = args;
            Debug.WriteLine($"VanSwitch (VistaSecurity) : Restaring as Elevated with args '{args}'");
            try
            {
                Process p = Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return;
            }

            Application.Exit();
        }
    }
}
