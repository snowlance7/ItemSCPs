using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using UnityEngine;
using WearableItemsAPI;
using static ItemSCPs.Plugin;
// UPDATE: Do singleton or maxSpawned using transpiler on spawnscrapinlevel
namespace ItemSCPs.SCP
{
    internal class SCP714Behavior : WearableObject, ISCP//, ISingletonItem // UPDATE: Make it so a eyes closing animation plays over the players hud, and eventually make it so they can fall asleep and have to spam buttons to wake up, they should also have no stamina and constantly be exhausted
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public static bool localPlayerAffected { get; private set; }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.07f, 0.1f, 0f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;

            wearableItemProperties.showWearableOnClient = true;
            wearableItemProperties.showWearable = true;

            wearableItemProperties.useLocalOffsets = true;
            wearableItemProperties.boneTransform = "spine.001/spine.002/spine.003/shoulder.R/arm.R_upper/arm.R_lower/hand.R/finger4.R/finger4.R.001";
            wearableItemProperties.boneTransformLocal = "metarig/spine.003/shoulder.R/arm.R_upper/arm.R_lower/hand.R/finger4.R/finger4.R.001";
            wearableItemProperties.wornPositionOffsetLocal = new Vector3(0.02f, -0.03f, 0f);
            wearableItemProperties.wornRotationOffsetLocal = new Vector3(0, 0, 20);
            wearableItemProperties.wornPositionOffset = new Vector3(0.03f, -0.06f, -0.01f);
            wearableItemProperties.wornRotationOffset = new Vector3(0, 0, 30);
        }

        public override void Update()
        {
            base.Update();

            if (playerWornBy == null) { return; }

            playerWornBy.insanityLevel = 0;
            playerWornBy.drunkness = 0;

            playerWornBy.sprintMeter = 0f;
            playerWornBy.isExhausted = true;
            VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, 0.1f));
        }

        public override void OnWear()
        {
            base.OnWear();
            if (localPlayer == playerWornBy)
            {
                localPlayerAffected = true;
                SCPEvents.LocalPlayerWearingSCP714 = true;
            }
        }

        public override void OnUnWear()
        {
            base.OnUnWear();
            if (localPlayer == lastPlayerWornBy)
            {
                localPlayerAffected = false;
                SCPEvents.LocalPlayerWearingSCP714 = false;
            }
        }
    }
}