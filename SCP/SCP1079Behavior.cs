using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;
using static SnowyLib.StatusEffectController;

namespace ItemSCPs.SCP
{
    internal class SCP1079Behavior : PhysicsProp // TODO: Add bloody footprints
    {
#pragma warning disable CS8618
        public AudioClip[] chewingSounds;
        public GameObject pinkBloodSplatterProjectorPrefab;
        public AudioClip sizzleSFX;
#pragma warning restore CS8618

        public static Dictionary<PlayerControllerB, List<GameObject>> PlayerPinkBodyBloodDecals = new Dictionary<PlayerControllerB, List<GameObject>>();

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

            if (TESTING.immunity) { return; }

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

                localPlayer.StatusEffectController().ApplyEffect(new DistributedActionEffect(() =>
                {
                    logger.LogDebug("Dropping blood");
                    Vector3 pos = localPlayer.gameplayCamera.transform.position.GetFloorPosition();
                    ItemSCPsNetworkHandler.Instance.DropPinkBloodServerRpc(pos);
                }, bloodDropAmount, "SCP-1079", "Dropping Pink Blood", 10f, (existing, incoming) => ConflictResult.Replace)); // TODO: Test if this still works after item is destroyed

                ItemSCPsNetworkHandler.Instance.AddPinkBloodToBodyServerRpc(localPlayer.actualClientId);

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

            }, "SCP-1079", "Pink Blood Secretion", UnityEngine.Random.Range(10f, 15f), onConflict: (existing, incoming) => ConflictResult.Replace));
        }

        public static void DropPinkBloodOnLocalClient(Vector3 pos)
        {
            GameObject? pinkBloodSplatterProjectorPrefab = ItemSCPsContentHandler.Instance.SCP1079?.PinkBloodDecalProjector;
            if (pinkBloodSplatterProjectorPrefab == null) { logger.LogError("PinkBloodSplatterProjector is null"); return; }
            GameObject bloodProjectorObj = Instantiate(pinkBloodSplatterProjectorPrefab, pos, pinkBloodSplatterProjectorPrefab.transform.rotation);
            DecalProjector bloodProjector = bloodProjectorObj.GetComponent<DecalProjector>();
            bloodProjector.enabled = true;
            Destroy(bloodProjectorObj, 300);
        }

        public static void AddPinkBloodToBodyOnLocalClient(PlayerControllerB player)
        {
            Material? pinkBloodDecal = ItemSCPsContentHandler.Instance.SCP1079?.PinkBloodDecal;
            if (pinkBloodDecal == null) { logger.LogError("Couldnt get pinkBloodDecal in content container"); return; }

            if (!PlayerPinkBodyBloodDecals.ContainsKey(player))
            {
                logger.LogDebug("No pink blood decals in dictionary, adding them");
                PlayerPinkBodyBloodDecals.Add(player, new List<GameObject>());

                foreach (var bodyBloodDecal in player.bodyBloodDecals)
                {
                    GameObject pinkBodyBloodDecal = Instantiate(bodyBloodDecal, bodyBloodDecal.transform.parent);
                    pinkBodyBloodDecal.name = "Pink" + bodyBloodDecal.name;
                    pinkBodyBloodDecal.GetComponent<DecalProjector>().material = pinkBloodDecal;
                    PlayerPinkBodyBloodDecals[player].Add(pinkBodyBloodDecal);
                }
            }

            foreach (var pinkBodyBloodDecal in PlayerPinkBodyBloodDecals[player])
            {
                if (!pinkBodyBloodDecal.activeSelf)
                {
                    pinkBodyBloodDecal.SetActive(true);
                }
            }
        }
    }

    [HarmonyPatch]
    internal class SCP1079Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ResetPlayerBloodObjects))]
        private static void ResetPlayerBloodObjectsPostfix(PlayerControllerB __instance, bool resetBodyBlood)
        {
            try
            {
                if (!resetBodyBlood) { return; }
                if (!SCP1079Behavior.PlayerPinkBodyBloodDecals.ContainsKey(__instance)) { return; }
                foreach (var pinkBodyBloodDecal in SCP1079Behavior.PlayerPinkBodyBloodDecals[__instance])
                {
                    pinkBodyBloodDecal.SetActive(false);
                }
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
                if (!SCP1079Behavior.PlayerPinkBodyBloodDecals.ContainsKey(__instance)) { return; }
                foreach (var pinkBodyBloodDecal in SCP1079Behavior.PlayerPinkBodyBloodDecals[__instance])
                {
                    pinkBodyBloodDecal.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}