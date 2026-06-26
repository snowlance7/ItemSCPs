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
            SCPDatabase = ItemSCPsContentHandler.Instance.ItemSCPsAssets!.SCPDatabase.SCPs;

            foreach (var scp in SCPDatabase)
            {
                MaxSpawnCounts.Add(scp.ItemNumber, Instance.Config.Bind<int>($"{scp.ItemNumber} Options", "Max Count", 1, "Max amount of this item that can exist at a time").Value);
            }
        }
    }
}
