using BepInEx;
using BepInEx.Configuration;
using Dawn;
using PSCPLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public static class Configs
    {
        public static List<SCPInfo> SCPDatabase = new List<SCPInfo>();
        public static Dictionary<string, int> MaxSpawnCounts = new Dictionary<string, int>(); // TODO
        public static void Init()
        {

        }
    }
}
