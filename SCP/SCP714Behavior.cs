using UnityEngine;
using WearableItemsAPI;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP714Behavior : WearableObject // TODO: Make the player tired and exhausted
    {
        public static bool localPlayerAffected { get; private set; }

        public void Awake() // TODO: Set these
        {
            itemProperties.positionOffset = new Vector3(0.07f, 0.1f, 0f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;

            wearableItemProperties.showWearableOnClient = false;
            wearableItemProperties.showWearable = false;
            //wearableItemProperties.wornPositionOffset = new Vector3(0, 0, 0);
            //wearableItemProperties.wornRotationOffset = new Vector3(0, 0, 0);
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