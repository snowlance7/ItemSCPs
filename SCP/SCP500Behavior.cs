using Dawn.Utils;
using ItemSCPs.SCP;
using PSCPLibrary;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    internal class SCP500Behavior : PhysicsProp // TODO: Gulp SFX not playing
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

        public SCPInfo scpInfo = null!;
        public List<GameObject> pillsInBottle = null!;
        public AudioClip pillSwallowSFX = null!;

        BoundedRange pillAmountRange = new BoundedRange(2, 15);

        public void Awake() // TODO
        {
            itemProperties.positionOffset = new Vector3(-0.08f, 0.11f, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, -90);
            itemProperties.floorYOffset = 90;
        }

        public override void Start()
        {
            base.Start();

            int pillAmount = (int)pillAmountRange.GetRandomInRange(Utils.randomGlobal);

            int pillsToTakeOut = pillsInBottle.Count - pillAmount;

            for (int i = 0; i < pillsToTakeOut; i++)
            {
                RemovePillFromBottle();
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown && !itemUsedUp)
            {
                RemovePillFromBottleServerRpc();
                TakePill();
                playerHeldBy.itemAudio.PlayOneShot(pillSwallowSFX, 1f);
            }
        }

        void TakePill()
        {
            localPlayer.StatusEffectController().RemoveEffect(x => x.curable);

            SCPEvents.OnSCP500TakenByLocalPlayer.Invoke();

            localPlayer.drunkness = 0;

            localPlayer.insanityLevel = 0f;

            localPlayer.health = 100;
            HUDManager.Instance.UpdateHealthUI(100, false);

            localPlayer.MakeCriticallyInjured(false);

            CadaverGrowthAI cadaverGrowthAI = FindObjectOfType<CadaverGrowthAI>();
            if (cadaverGrowthAI != null)
            {
                cadaverGrowthAI.CurePlayerRpc((int)localPlayer.actualClientId); // TODO: Test this
            }
        }

        void RemovePillFromBottle()
        {
            GameObject pill = pillsInBottle.Last();
            pillsInBottle.Remove(pill);
            Destroy(pill);

            if (pillsInBottle.Count == 0)
            {
                itemUsedUp = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemovePillFromBottleServerRpc()
        {
            if (!IsServer) { return; }
            RemovePillFromBottleClientRpc();
        }

        [ClientRpc]
        public void RemovePillFromBottleClientRpc()
        {
            RemovePillFromBottle();
        }
    }
}
