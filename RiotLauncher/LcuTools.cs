using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RiotLauncher
{
    internal static class LcuTools
    {
        public static string GetRiotClientPath()
        {
            // Find the RiotClientInstalls file.
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Riot Games/RiotClientInstalls.json");
            if (!File.Exists(installPath)) return null;

            var data = JObject.Parse(File.ReadAllText(installPath));
            var rcPaths = new List<string>();
            
            if (data.ContainsKey("rc_default")) rcPaths.Add(data["rc_default"].ToString());
            if (data.ContainsKey("rc_live")) rcPaths.Add(data["rc_live"].ToString());
            if (data.ContainsKey("rc_beta")) rcPaths.Add(data["rc_beta"].ToString());

            return rcPaths.FirstOrDefault(File.Exists);
        }
    }
}