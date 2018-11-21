using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace Fallout76Proxy
{
    class BethesdaLauncherMissedException : Exception { public BethesdaLauncherMissedException(string message) : base(message) { } };
    class Fallout76NotStartedException : Exception { public Fallout76NotStartedException(string message) : base(message) { } };
    class Fallout76TooManyStartedException : Exception { public Fallout76TooManyStartedException(string message) : base(message) { } };
    class Fallout76StrangeArguments : Exception { public Fallout76StrangeArguments(string message) : base(message) { } };

    class Program
    {
        static string GetCommandLine()
        {
            ManagementClass mngmtClass = new ManagementClass("Win32_Process");
            foreach (ManagementObject o in mngmtClass.GetInstances())
            {
                if (o["Name"].Equals("Fallout76.exe"))
                {
                    return (string) o["CommandLine"];
                }
            }

            throw new Fallout76NotStartedException("Can't get Fallout76 arguments");
        }

        static bool Fallout76Exists()
        {
            Process[] fallouts76 = Process.GetProcessesByName("Fallout76");
            return fallouts76.Count() > 0;
        }

        static void Launch()
        {
            RegistryKey bethesdaNet = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\BethesdaNet\\Shell\\Open\\Command");

            if (bethesdaNet == null)
                throw new BethesdaLauncherMissedException("Try to reinstall bethesda launcher.");

            Console.WriteLine("Starting Fallout76 from Bethesda launcher.");

            Process.Start("bethesdanet://run/20");

            Console.WriteLine("Waiting for Fallout started.");

            while (!Fallout76Exists())
            {
                Thread.Sleep(500);
            }

            Console.WriteLine("Restarting Fallout 76 as child process.");

            Process[] fallout76 = Process.GetProcessesByName("Fallout76");

            if (fallout76.Count() == 0)
                throw new Fallout76NotStartedException("For some reason Fallout76 not started.");

            // Lolwut
            if (fallout76.Count() > 1)
                throw new Fallout76TooManyStartedException("Too many Fallout76 launched. Stop others!");

            // In order to Steam Overlay working we must launch F76 with token
            Regex regex = new Regex("\"(.+?)\"\\s(.+)");
            Match match = regex.Match(GetCommandLine());

            fallout76.First().Kill();

            if (match.Groups.Count == 0)
                throw new Fallout76StrangeArguments("For some reason Fallout76 have no token!");

            string Fallout76Path = match.Groups[1].Value;

            Directory.SetCurrentDirectory(Path.GetDirectoryName(Fallout76Path));

            // Now starting child Fallout76
            Process process = new Process();
            process.StartInfo.FileName = Fallout76Path;
            process.StartInfo.Arguments = match.Groups[2].Value;
            process.Start();

            Console.WriteLine("Waiting for Fallout 76 closed.");

            process.WaitForExit();

            Console.WriteLine("Closing Bethesda launcher.");
            foreach (Process bethesdaLauncher in Process.GetProcessesByName("BethesdaNetLauncher"))
                bethesdaLauncher.Kill();
        }

        static void Main(string[] args)
        {
            try
            {
                Launch();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("\nPress any key to exit...");
                Console.Read();
            }
        }
    }
}
