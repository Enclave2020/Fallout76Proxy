using Microsoft.Win32;
using System.Diagnostics;
using System.Linq;

namespace Fallout76Proxy
{
    static class BethesdaLauncher
    {
        public static bool Installed()
        {
            RegistryKey bethesdaNet = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\BethesdaNet\\Shell\\Open\\Command");

            return bethesdaNet != null;
        }

        public static void Start(int GameIdx)
        {
            Process.Start($"bethesdanet://run/{GameIdx}");
        }

        public static void Stop()
        {
            foreach (Process bethesdaLauncher in Process.GetProcessesByName("BethesdaNetLauncher"))
                bethesdaLauncher.Kill();
        }

        public static bool Active()
        {
            return Process.GetProcessesByName("BethesdaNetLauncher").Count() > 0;
        }
    }
}
