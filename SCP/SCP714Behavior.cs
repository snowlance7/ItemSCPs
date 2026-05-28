using UnityEngine;
using WearableItemsAPI;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP714Behavior : WearableObject
    {
        public static bool localPlayerAffected { get; private set; }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;
        }

        public override void Update()
        {
            base.Update();

            if (playerWornBy == null) { return; }

            playerWornBy.insanityLevel = 0;
            playerWornBy.drunkness = 0;
        }

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