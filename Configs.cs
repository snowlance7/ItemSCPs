using ItemSCPs.SCP;
using System.Collections.Generic;
using static ItemSCPs.Plugin;

// siteoneonetwolethalbundle:siteoneonetwoflow

namespace ItemSCPs
{
    public static class Configs
    {
        public static void Init()
        {
            SCP3482Behavior.InitConfigs();
            SCP420JBehavior.InitConfigs();
            SCP498Behavior.InitConfigs();
            SCP500Behavior.InitConfigs();
            SCP689Behavior.InitConfigs();
            SCP735Behavior.InitConfigs();
            SCP983Behavior.InitConfigs();
        }
    }
}
