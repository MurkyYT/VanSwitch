using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace VanSwitch
{
    public static class RegistryHelper
    {
        public static bool RemoveRegistryValue(string fullPath,RegistryHive registryHive = RegistryHive.CurrentUser, RegistryView registryView = RegistryView.Default)
        {
            try
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Trying to delete {fullPath} registry value");
                RegistryKey localKey = RegistryKey.OpenBaseKey(registryHive, registryView);
                string keyName = Path.GetDirectoryName(fullPath).Replace("Computer\\", "");
                string valueName = Path.GetFileName(fullPath);
                using (RegistryKey key = localKey.OpenSubKey(keyName, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(valueName);
                        Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Deleted registry value of: {fullPath}");
                        return true;
                    }
                }
                return false;
            }
            catch (System.Security.SecurityException)
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Error while deleting {fullPath} value, not enough rights");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Error while deleting {fullPath} value:\n{ex.StackTrace}");
                return false;
            }
        }
        public static bool SetRegistryValue(string fullPath, string value, RegistryHive registryHive = RegistryHive.CurrentUser, RegistryView registryView = RegistryView.Default)
        {
            try
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Trying to set {fullPath} registry value");
                RegistryKey localKey = RegistryKey.OpenBaseKey(registryHive, registryView);
                string keyName = Path.GetDirectoryName(fullPath).Replace("Computer\\", "");
                string valueName = Path.GetFileName(fullPath);
                using (RegistryKey key = localKey.OpenSubKey(keyName, true))
                {
                    if (key != null)
                    {
                        key.SetValue(valueName, value);
                        Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Set registry value of: {fullPath}");
                        return true;
                    }
                }
                return false;
            }
            catch (System.Security.SecurityException)
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Error while setting {fullPath} value, not enough rights");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Error while setting {fullPath} value:\n{ex.StackTrace}");
                return false;
            }
        }
        public static object GetRegistryValue(string fullPath, RegistryHive registryHive = RegistryHive.CurrentUser, RegistryView registryView = RegistryView.Default)
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(registryHive, registryView);
            object value = null;
            try
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Trying to find {fullPath} registry value");
                string keyName = Path.GetDirectoryName(fullPath).Replace("Computer\\", "");
                string valueName = Path.GetFileName(fullPath);
                using (RegistryKey key = localKey.OpenSubKey(keyName))
                {
                    if (key != null)
                    {
                        value = key.GetValue(valueName);
                        if(value == null)
                            Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Registry value of: {fullPath} is empty");
                        else
                            Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Registry value of: {fullPath} = {value}");
                        return value;
                    }
                    Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Key {keyName} = null");
                    return value;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"VanSwitch (RegistryHelper) : " + $"Error ocurred while searching for registry value of {fullPath}:\n{ex.StackTrace}");
                return value;
            }

        }
    }
}
