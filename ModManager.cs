using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using SharpBSABA2;
using SharpBSABA2.BA2Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Fallout76Proxy
{
    class ModManager
    {
        /* STRANGE MOD LOADING
        https://www.nexusmods.com/fallout76/mods/162
        https://www.nexusmods.com/fallout76/mods/60
        https://www.nexusmods.com/fallout76/mods/183
        https://www.nexusmods.com/fallout76/mods/91
        */

        class ModConflictException : Exception { public ModConflictException(string message) : base(message) { } };
        class Fallout76NotInstalled : Exception { public Fallout76NotInstalled(string message) : base(message) { } };

        enum LoadType
        {
            Default,
            StartUp
        }

        LoadType GetLoadType(BA2 ba2)
        {
            foreach (ArchiveEntry file in ba2.Files)
            {
                if (file.FileName.Contains("fonts_"))
                {
                    return LoadType.StartUp;
                }
            }

            return LoadType.Default;
        }

        string GetFallout76Path()
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Fallout 76");
            if (registryKey == null)
            {
                throw new Fallout76NotInstalled("Can't detect Fallout 76 installation!");
            }

            string path = (string) registryKey.GetValue("Path");

            if (String.IsNullOrEmpty(path))
            {
                throw new Fallout76NotInstalled("Can't detect Fallout 76 directory!");
            }

            path = path.Replace("\"", "");

            if (!Directory.Exists($"{path}\\Data"))
            {
                throw new Fallout76NotInstalled("Fallout 76 data directory missed!");
            }

            return path;
         }

        public void Process()
        {
            Dictionary<string, string> conflicts = new Dictionary<string, string>();

            string sResourceArchive2List = "SeventySix - ATX_Main.ba2, SeventySix - ATX_Textures.ba2";
            string sResourceStartUpArchiveList = "SeventySix - Interface.ba2, SeventySix - Localization.ba2, SeventySix - Shaders.ba2, SeventySix - Startup.ba2";

            string[] ba2Archives = Directory.GetFiles($"{GetFallout76Path()}\\Data\\", "*.ba2");

            Console.WriteLine($"Detected {ba2Archives.Length} mods.");

            foreach (string ba2Archive in ba2Archives)
            {
                // skip F76 files
                if (ba2Archive.Contains("SeventySix -"))
                {
                    continue;
                }

                BA2 ba2 = new BA2(ba2Archive);

                foreach (ArchiveEntry file in ba2.Files)
                {
                    if (conflicts.ContainsKey(file.FullPath))
                    {
                        throw new ModConflictException(String.Format("Mod {0} conflicts with {1}. Remove one of them.", ba2.FileName, conflicts[file.FullPath]));
                    }

                    conflicts.Add(file.FullPath, ba2.FileName);
                }

                LoadType modLoadType = GetLoadType(ba2);
                switch (modLoadType)
                {
                    case LoadType.Default:
                        sResourceArchive2List += String.Format(", {0}", ba2.FileName);
                        break;

                    case LoadType.StartUp:
                        sResourceStartUpArchiveList += String.Format(", {0}", ba2.FileName);
                        break;
                }
            }

            WriteFallout76Customs(sResourceArchive2List, sResourceStartUpArchiveList);
        }

        void WriteFallout76Customs(string sResourceArchive2List, string sResourceStartUpArchiveList)
        {
            FileIniDataParser parser = new FileIniDataParser();

            string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fallout76CustomPath = $"{myDocumentsPath}\\My Games\\Fallout 76\\Fallout76Custom.ini";

            IniData data = new IniData();

            if (File.Exists(fallout76CustomPath))
            {
                // Remove readonly
                File.SetAttributes(fallout76CustomPath, FileAttributes.Normal);

                data = parser.ReadFile(fallout76CustomPath);
            }

            data["Archive"]["sResourceArchive2List"] = sResourceArchive2List;
            data["Archive"]["sResourceStartUpArchiveList"] = sResourceStartUpArchiveList;

            // Empty old data
            data["Archive"].RemoveKey("sResourceArchiveList2");
            data["Archive"].RemoveKey("sResourceIndexFileList");

            File.WriteAllText(fallout76CustomPath, data.ToString());
        }
    }
}
