using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace Fallout76Proxy
{
    class GameManager
    {
        readonly string processName;

        public GameManager(string processName)
        {
            this.processName = processName;
        }

        public void WaitFor()
        {
            Process[] processes;

            do
            {
                processes = Process.GetProcessesByName(processName);
            }
            while (processes.Count() == 0);
        }

        public void RestartAsChild()
        {
            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Count() == 0)
                throw new NotStartedException($"For some reason {processName} not started.");

            if (processes.Count() > 1)
                throw new TooManyStartedException($"Too many {processName} launched. Stop others!");

            Regex regex = new Regex("\"(.+?)\"\\s(.+)");
            Match match = regex.Match(GetCommandLine($"{processName}.exe"));

            processes.First().Kill();

            if (match.Groups.Count == 0)
                throw new StrangeArguments($"For some reason {processName} have no token!");

            string TargetPath = match.Groups[1].Value;
            string TargetArguments = match.Groups[2].Value;

            Directory.SetCurrentDirectory(Path.GetDirectoryName(TargetPath));

            Process process = new Process();
            process.StartInfo.FileName = TargetPath;
            process.StartInfo.Arguments = TargetArguments;
            process.Start();
        }

        string GetCommandLine(string processName)
        {
            ManagementClass mngmtClass = new ManagementClass("Win32_Process");
            foreach (ManagementObject o in mngmtClass.GetInstances())
            {
                if (o["Name"].Equals(processName))
                {
                    return (string)o["CommandLine"];
                }
            }

            throw new NotStartedException($"Can't get {processName} arguments");
        }
    }
}
