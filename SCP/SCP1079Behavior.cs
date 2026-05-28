using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;
using static SnowyLib.StatusEffectController;

namespace ItemSCPs.SCP
{
    internal class SCP1079Behavior : PhysicsProp // TODO: Add body blood
    {
#pragma warning disable CS8618
        public AudioClip[] chewingSounds;
        public GameObject pinkBloodSplatterProjectorPrefab;
        public AudioClip sizzleSFX;
#pragma warning restore CS8618

        static int candiesEatenByLocalPlayer = 0;

        float timeSinceCandyDecrement;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.11f, -0.05f);
            itemProperties.rotationOffset = new Vector3(180f, 90f, -15f);
            itemProperties.floorYOffset = 90;

            itemProperties.canBeGrabbedBeforeGameStart = true;
            itemProperties.canBeInspected = true;
            itemProperties.toolTips = ["Eat Candy [LMB]"];
        }

        public override void Update()
        {
            base.Update();

            if (candiesEatenByLocalPlayer > 0)
            {
                timeSinceCandyDecrement += Time.deltaTime;

                if (timeSinceCandyDecrement > 60f)
                {
                    timeSinceCandyDecrement = 0f;
                    candiesEatenByLocalPlayer--;
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            RoundManager.PlayRandomClip(playerHeldBy.itemAudio, chewingSounds);
            candiesEatenByLocalPlayer++;

            int bloodDropAmount = 2 * candiesEatenByLocalPlayer;
            AudioClip sizzle = sizzleSFX;

            localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
            {
                float peakSizzle = Mathf.Clamp01(0.5f + (candiesEatenByLocalPlayer * 0.15f));
                logger.LogDebug($"Peak sizzle: {peakSizzle}");

                AnimationCurve sizzleCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, peakSizzle), new Keyframe(0, 0));

                localPlayer.itemAudio.clip = sizzleSFX;
                localPlayer.itemAudio.volume = 0.5f;
                localPlayer.itemAudio.Play();

                localPlayer.StatusEffectController().ApplyEffect(new CurveValueEffect((value) => localPlayer.itemAudio.volume = value, sizzleCurve, 10f, "SCP-1079", "Blood Boiling", (existing, incoming) => ConflictResult.Replace, (effect) =>
                {
                    localPlayer.itemAudio.volume = 1f;
                }));

                localPlayer.StatusEffectController().ApplyEffect(new DistributedActionEffect(() => DropBlood(), bloodDropAmount, "SCP-1079", "Pink Blood Secretion", 10f, (existing, incoming) => ConflictResult.Replace));

                int damagePerSecond = Mathf.RoundToInt(Mathf.Pow(2.5f, candiesEatenByLocalPlayer) / 10);
                if (damagePerSecond == 0) { return; }
                logger.LogDebug($"Doing {damagePerSecond} damage per second");

                localPlayer.StatusEffectController().ApplyEffect(new IntervalActionEffect(1f, () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(damagePerSecond, false);
                    localPlayer.inSpecialInteractAnimation = false;
                    localPlayer.bleedingHeavily = false;
                }, "SCP-1079", "Blood Loss Damage", 10f, (existing, incoming) => ConflictResult.Replace));

            }, "SCP-1079", "Pink Blood Secretion", UnityEngine.Random.Range(10f, 15f), onConflict: (existing, incoming) => ConflictResult.Deny));
        }

        public void DropBlood()
        {
            logger.LogDebug("Dropping blood");
            Vector3 pos = localPlayer.gameplayCamera.transform.position.GetFloorPosition();
            DropBloodServerRpc(pos);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropBloodServerRpc(Vector3 pos)
        {
            if (!IsServer) { return; }
            DropBloodClientRpc(pos);
        }

        [ClientRpc]
        public void DropBloodClientRpc(Vector3 pos)
        {
            GameObject bloodProjectorObj = Instantiate(pinkBloodSplatterProjectorPrefab, pos, pinkBloodSplatterProjectorPrefab.transform.rotation);
            DecalProjector bloodProjector = bloodProjectorObj.GetComponent<DecalProjector>();
            bloodProjector.enabled = true;
            Destroy(bloodProjectorObj, 300);
        }
    }

    /*[HarmonyPatch]
    internal class SCP1079Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ResetPlayerBloodObjects))]
        private static void ResetPlayerBloodObjectsPostfix(PlayerControllerB __instance, bool resetBodyBlood)
        {
            try
            {
                //PinkBloodManager.Instance(__instance)?.ResetPlayerBloodObjects(resetBodyBlood);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.RemoveBloodFromBody))]
        private static void RemoveBloodFromBodyPostfix(PlayerControllerB __instance)
        {
            try
            {
                //PinkBloodManager.Instance(__instance)?.AddBloodToBody(false);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }*/
}