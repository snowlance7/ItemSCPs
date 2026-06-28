using System.Collections.Generic;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public static class Configs
    {
        public static Dictionary<string, bool> MultipleInstances = new Dictionary<string, bool>(); // TODO
        public static void Init()
        {
            MultipleInstances.Add("SCP-005", Instance.Config.Bind($"SCP-005 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-012", Instance.Config.Bind($"SCP-012 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-1025", Instance.Config.Bind($"SCP-1025 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-1079", Instance.Config.Bind($"SCP-1079 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-268", Instance.Config.Bind($"SCP-268 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-3482", Instance.Config.Bind($"SCP-3482 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-498", Instance.Config.Bind($"SCP-498 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-500", Instance.Config.Bind($"SCP-500 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-714", Instance.Config.Bind($"SCP-714 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-735", Instance.Config.Bind($"SCP-735 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
            MultipleInstances.Add("SCP-983", Instance.Config.Bind($"SCP-983 Options", "Enable Multiple Instances", false, "If true, this SCP can have multiple instances spawned at a time").Value);
        }
    }
}
