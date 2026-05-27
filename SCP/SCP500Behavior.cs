using BepInEx.Logging;
using Dawn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using static ItemSCPs.Plugin;
using SnowyLib;

namespace ItemSCPs
{
    internal class SCP500Behavior : PhysicsProp
    {
        public List<GameObject> pillsInBottle = null!;
        public AudioClip pillSwallowSFX = null!;

        BoundedRange pillAmountRange = new BoundedRange(2, 15);

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
            localPlayer.StatusEffectController().RemoveEffect(x => x.curableBySCP500);

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
