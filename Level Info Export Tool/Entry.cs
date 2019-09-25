using Newtonsoft.Json;
using Spectrum.API.Configuration;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using Spectrum.API.IPC;
using Spectrum.API.Logging;
using Spectrum.API.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Level_Info_Export_Tool
{
    public class Entry : IPlugin
    {
        public static FileSystem Files = new FileSystem();
        public static Logger Log = new Logger("Output")
        {
            WriteToConsole = true,
            ColorizeLines = true
        };
        public static Dictionary<string, object> Flags = new Dictionary<string, object>();

        public static Dictionary<string, object> Export = new Dictionary<string, object>();

        public void Initialize(IManager manager, string ipcIdentifier)
        {
            string[] args = Environment.GetCommandLineArgs();

            foreach (var arg in args)
                if (arg.Length > 1)
                    SetFlag($"param__{arg.Substring(1).ToLower()}", true);

            ShowHelp();

            Events.Scene.LoadFinish.Subscribe((data) =>
            {
                if (data.sceneName != "MainMenu" && GetFlag("run__generate", true)) return;

                Generate();

                if (GetFlag("param__closeafterexport", false))
                    UnityEngine.Application.Quit();

                SetFlag("run__generate", true);
            });
        }

        public static void ShowHelp()
        {
            if (GetFlag("param__help", false))
            {
                Log.Warning($"===== Level Info Export Tool Help =====");
                Log.Info($"Command line arguments:");
                Log.Info($"\t-closeafterexport");
                Log.Info($"\t Closes the game after the json file has been generated.");
                Log.Info($"\t-formatjson");
                Log.Info($"\t Adds line breaks to the json file instead of making everything fit on a single line.");
                Log.Warning($"=======================================");
            }
        }

        public static void Generate()
        {
            try
            {
                LevelSetsManager lsm = G.Sys.LevelSets_;

                Export.Clear();

                Export.Add("OfficialLevels", lsm.OfficialLevelInfosList_.ToList().Where((item) => item.relativePath_.ToLower().StartsWith("officiallevels/")));
                Export.Add("CommunityLevels", lsm.OfficialLevelInfosList_.ToList().Where((item) => item.relativePath_.ToLower().StartsWith("communitylevels/")));

                SaveExportData();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static void SaveExportData()
        {
            var Root = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var SettingsDirectory = $"{Root}\\Export";
            var FilePath = $"{SettingsDirectory}\\export.json";

            try
            {
                if (!Directory.Exists(SettingsDirectory))
                    Directory.CreateDirectory(SettingsDirectory);
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                JsonSerializer SERIALIZER = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Include,
                };
                using (StreamWriter FILE_STREAM = new StreamWriter(FilePath))
                    using (JsonWriter JSON_WRITER = new JsonTextWriter(FILE_STREAM))
                    {
                        JSON_WRITER.Formatting = GetFlag("param__formatjson", false) ? Formatting.Indented : Formatting.None;
                        SERIALIZER.Serialize(JSON_WRITER, Export);
                    }
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        public static void SetFlag(string key, object value)
        {
            Flags[key] = value;
        }

        public static object GetFlag(string key, object notfound = null)
        {
            if (Flags.TryGetValue(key, out object found))
                return found;
            return notfound;
        }
        public static T GetFlag<T>(string key) => (T)GetFlag(key);
        public static T GetFlag<T>(string key, T notfound) => (T)GetFlag(key, (object)notfound);
    }
}
