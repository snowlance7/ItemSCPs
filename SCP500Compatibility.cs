using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using static ItemSCPs.Plugin;
using HarmonyLib;
using GameNetcodeStuff;
using ItemSCPs.Items.Snowy;

namespace ItemSCPs
{
    internal class SCP500Compatibility
    {
        private static bool? _enabled;

        internal static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ProjectSCP.SCP500");
                }
                return (bool)_enabled;
            }
        }

        internal static bool IsLocalPlayerAffectedBySCP500
        {
            get
            {
                if (enabled)
                {
                    return LocalPlayerAffectedBySCP500();
                }
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool LocalPlayerAffectedBySCP500() // TODO
        {
            return false;
            //return SCP500.SCP500Controller.LocalPlayerAffectedBySCP500;
        }
    }

    /*[HarmonyPatch()]
    internal class SCP500Patch
    {
        static SCP500Patch()
        {
            LoggerInstance.LogDebug("Loading SCP500Patch");
            if (!SCP500Compatibility.enabled)
            {
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("SCP500Controller", "TakePill")]
        //[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TakePillPostfix()
        {
            if (SCP500Compatibility.enabled)
            {
                LoggerInstance.LogDebug("Player took SCP500 pill");
            }
        }
    }*/
}