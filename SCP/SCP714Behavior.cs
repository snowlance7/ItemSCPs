using GameNetcodeStuff;
using WearableItemsAPI;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP714Behavior : WearableObject
    {
        public static bool localPlayerAffected;

        public override void OnWear()
        {
            base.OnWear();
            if (localPlayer == playerWornBy)
                localPlayerAffected = true;
        }

        public override void OnUnWear()
        {
            base.OnUnWear();
            if (localPlayer == lastPlayerWornBy)
                localPlayerAffected = false;
        }
    }
}
