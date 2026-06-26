using PSCPLibrary;
using UnityEngine;
using WearableItemsAPI;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP714Behavior : WearableObject // TODO: Make it so a eyes closing animation plays over the players hud, and eventually make it so they can fall asleep and have to spam buttons to wake up, they should also have no stamina and constantly be exhausted // TODO: Make the player tired and exhausted // TODO: Set up wearable offsets correctly, rework wearableitemsapi?
    {
        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            int maxCount = Configs.MaxSpawnCounts[itemProperties.itemName];
            int spawnCount = FindObjectsOfType<SCP005Behavior>().Length;
            if (spawnCount <= maxCount) { return; }
            logger.LogDebug($"Only {maxCount} {itemProperties.name} instance{(maxCount > 1 ? "s" : "")} can be spawned, despawning duplicate");
            NetworkObject.Despawn(destroy: true);
        }

        public static bool localPlayerAffected { get; private set; }

        public void Awake() // TODO: Set these
        {
            itemProperties.positionOffset = new Vector3(0.07f, 0.1f, 0f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;

            wearableItemProperties.showWearableOnClient = false;
            wearableItemProperties.showWearable = false;

            wearableItemProperties.boneTransform = "";
            wearableItemProperties.boneTransformLocal = ""; // TODO: Set these up
            //wearableItemProperties.wornPositionOffset = new Vector3(0, 0, 0);
            //wearableItemProperties.wornRotationOffset = new Vector3(0, 0, 0);
        }

        public override void Update()
        {
            base.Update();

            if (playerWornBy == null) { return; }

            playerWornBy.insanityLevel = 0;
            playerWornBy.drunkness = 0;
            playerWornBy.isExhausted = true; // TODO: Test this
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