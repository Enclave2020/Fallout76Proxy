using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;

namespace Fallout76Proxy
{
    class BethesdaLauncherMissedException : Exception { public BethesdaLauncherMissedException(string message) : base(message) { } };
    class NotStartedException : Exception { public NotStartedException(string message) : base(message) { } };
    class TooManyStartedException : Exception { public TooManyStartedException(string message) : base(message) { } };
    class StrangeArguments : Exception { public StrangeArguments(string message) : base(message) { } };

    class Program
    {
        enum ModEnablerState
        {
            None,
            Disabled,
            Enabled
        }

        static string GetCommandLine(string ProcessName)
        {
            ManagementClass mngmtClass = new ManagementClass("Win32_Process");
            foreach (ManagementObject o in mngmtClass.GetInstances())
            {
                if (o["Name"].Equals(ProcessName))
                {
                    return (string) o["CommandLine"];
                }
            }

            throw new NotStartedException("Can't get Fallout76 arguments");
        }

        static bool Fallout76Exists()
        {
            Process[] fallouts76 = Process.GetProcessesByName("Fallout76");
            return fallouts76.Count() > 0;
        }

        static ModEnablerState GetModEnablerState()
        {
            if (!File.Exists("mods.txt"))
            {
                return ModEnablerState.None;
            }

            string content = File.ReadAllText("mods.txt");

            ModEnablerState modEnablerState = (ModEnablerState) Enum.Parse(typeof(ModEnablerState), content);

            return modEnablerState;
        }

        static void SetModEnablerState(ModEnablerState modEnablerState)
        {
            string content = modEnablerState.ToString();

            File.WriteAllText("mods.txt", content);
        }

        static void ModsManagerProcess()
        {
            if (GetModEnablerState() == ModEnablerState.None)
            {
                Console.WriteLine("Would you like to try mods enabler?");
                Console.WriteLine("It will be add all BA2 mods from game folder to Fallout76Custom at each start.");
                Console.WriteLine("Also it detecting mods conflicts.");
                Console.WriteLine("\nWrite Y to enable this feature or anything else to skip.");
                Console.WriteLine("You can change it anytime by removing mods.txt near this exe.");

                ConsoleKeyInfo userChoose = Console.ReadKey();
                Console.WriteLine();

                if (userChoose.Key == ConsoleKey.Y)
                {
                    SetModEnablerState(ModEnablerState.Enabled);
                }
                else
                {
                    SetModEnablerState(ModEnablerState.Disabled);
                }
            }

            if (GetModEnablerState() == ModEnablerState.Enabled)
            {
                ModManager modManager = new ModManager();
                modManager.Process();
            }
        }

        static void Launch()
        {
            Console.Title = "Fallout76 Steam overlay";

            ModsManagerProcess();

            if (!BethesdaLauncher.Installed())
                throw new BethesdaLauncherMissedException("Try to reinstall bethesda launcher.");

            Console.WriteLine("Starting Fallout76 from BethesdaLauncher.");

            BethesdaLauncher.Start(BethesdaGames.Fallout76);

            Console.WriteLine("Waiting for game started.");

            GameManager fallout76 = new GameManager("Fallout76");

            fallout76.WaitFor();

            Console.WriteLine("Restarting Fallout 76 as child process.");

            fallout76.RestartAsChild();

            Console.WriteLine("Closing BethesdaLauncher.");
            BethesdaLauncher.Stop();
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
