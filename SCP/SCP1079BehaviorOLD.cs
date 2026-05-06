/*
using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP1079BehaviorOLD : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioClip[] ChewingSounds;
#pragma warning restore CS8618

        public static int candiesEatenByLocalPlayer;

        float timeSinceCandyEaten;

        const float candyResetCooldown = 120f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.11f, -0.05f);
            itemProperties.rotationOffset = new Vector3(180f, 90f, -15f);
            itemProperties.floorYOffset = 90;
        }

        public override void Update()
        {
            base.Update();
            timeSinceCandyEaten += Time.deltaTime;
            if (timeSinceCandyEaten > candyResetCooldown)
            {
                candiesEatenByLocalPlayer = 0;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            RoundManager.PlayRandomClip(playerHeldBy.statusEffectAudio, ChewingSounds);
            candiesEatenByLocalPlayer++;
            EatCandyServerRpc(playerHeldBy.actualClientId, candiesEatenByLocalPlayer);
        }

        [ServerRpc(RequireOwnership = false)]
        public void EatCandyServerRpc(ulong clientId, int candiesEaten)
        {
            if (!IsServer) { return; }
            EatCandyClientRpc(clientId, candiesEaten);
        }

        [ClientRpc]
        public void EatCandyClientRpc(ulong clientId, int candiesEaten)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { logger.LogError("Couldnt find player with id: " + clientId); return; }

            int damage = (int)(4f * Mathf.Pow(1.84f, candiesEaten - 1));
            int amount = 3 * candiesEaten * candiesEaten;
            float interval = 1.2f / Mathf.Pow(1.35f, candiesEaten - 1);

            logger.LogDebug("Dropping pink blood");
            PinkBloodManager.Instance(player)?.DropPinkBlood(amount, interval, candiesEaten <= 1 ? 0 : damage);
        }
    }

    internal class PinkBloodManager : MonoBehaviour
    {
        PlayerControllerB? player { get; set; }

        List<GameObject> playerPinkBloodPooledObjects = [];
        int currentBloodIndex;

        static List<DecalProjector> snowFootprintsPooledObjects = [];
        static int currentFootprintIndex;

        List<GameObject> bodyPinkBloodDecals = [];

        public static PinkBloodManager? Instance(PlayerControllerB player)
        {
            if (ItemSCPsContentHandler.Instance.SCP1079 == null) { return null; }

            if (!player.gameObject.TryGetComponent(out PinkBloodManager instance))
            {
                instance = player.gameObject.AddComponent<PinkBloodManager>();
                instance.Init(player);
            }

            return instance;
        }

        void Init(PlayerControllerB player)
        {
            this.player = player;

            foreach (var decal in player.bodyBloodDecals)
            {
                GameObject copy = Instantiate(decal, decal.transform.parent); // TODO: Test this
                copy.GetComponent<DecalProjector>().material.color = Color.magenta;
                copy.SetActive(false);
                bodyPinkBloodDecals.Add(copy);
            }
        }

        public void OnDestroy()
        {
            foreach (var decal in bodyPinkBloodDecals)
            {
                Destroy(decal);
            }
        }

        public void AddBloodToBody(bool value)
        {
            foreach (var decal in bodyPinkBloodDecals)
            {
                if (decal.activeSelf == value) { continue; }
                decal.SetActive(value);
            }
        }

        public void InstantiateBloodPooledObjects()
        {
            int num = 50;
            for (int i = 0; i < num; i++)
            {
                GameObject gameObject = Instantiate(StartOfRound.Instance.playerBloodPrefab, player!.playersManager.bloodObjectsContainer);
                gameObject.name = "PinkBloodPooledObject";
                gameObject.GetComponent<DecalProjector>().material = ItemSCPsContentHandler.Instance.SCP1079!.PinkBloodDecal;
                gameObject.SetActive(value: false);
                playerPinkBloodPooledObjects.Add(gameObject);
            }
        }

        public static void InstantiateFootprintsPooledObjects()
        {
            int num = 250;
            for (int i = 0; i < num; i++)
            {
                GameObject gameObject = Instantiate(StartOfRound.Instance.footprintDecal, StartOfRound.Instance.bloodObjectsContainer);
                var projector = gameObject.GetComponent<DecalProjector>();
                projector.material = ItemSCPsContentHandler.Instance.SCP1079!.PinkFootprintsDecal;
                projector.enabled = false;
                snowFootprintsPooledObjects.Add(projector);
            }
        }

        public void ResetPlayerBloodObjects(bool resetBodyBlood = true)
        {
            if (playerPinkBloodPooledObjects != null)
            {
                for (int i = 0; i < playerPinkBloodPooledObjects.Count; i++)
                {
                    playerPinkBloodPooledObjects[i].SetActive(value: false);
                }
            }
            if (resetBodyBlood)
            {
                AddBloodToBody(false);
            }
        }

        public static void ResetPooledObjects()
        {
            foreach (var item in snowFootprintsPooledObjects)
            {
                if (item == null) { continue; }
                item.enabled = false;
            }
        }

        public void DropPinkBlood(int amount, float interval, int damage = 0)
        {
            AddBloodToBody(true);
            StartCoroutine(DropPinkBloodRoutine(amount, interval, damage));
        }

        IEnumerator DropPinkBloodRoutine(int amount, float interval, int damage)
        {
            yield return null;

            for (int i = 0; i < amount; i++)
            {
                DropPinkBlood();
                yield return new WaitForSeconds(interval);
            }
        }

        void DropPinkBlood()
        {
            if (playerPinkBloodPooledObjects == null || player == null) { return; }
            bool flag = false;
            if (player.bloodDropTimer >= 0f && !player.isPlayerDead)
            {
                return;
            }
            player.bloodDropTimer = 0.4f;
            Vector3 direction = Vector3.down;
            Transform transform = playerPinkBloodPooledObjects[currentBloodIndex].transform;
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (player.isInElevator)
            {
                transform.SetParent(player.playersManager.elevatorTransform);
            }
            else
            {
                transform.SetParent(player.playersManager.bloodObjectsContainer);
            }
            if (player.isPlayerDead)
            {
                if (player.deadBody == null || !player.deadBody.gameObject.activeSelf)
                {
                    return;
                }
                player.interactRay = new Ray(player.deadBody.bodyParts[3].transform.position + Vector3.up * 0.5f, direction);
            }
            else
            {
                player.interactRay = new Ray(player.transform.position + player.transform.up * 2f, direction);
            }
            if (Physics.Raycast(player.interactRay, out player.hit, 6f, player.playersManager.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
            {
                flag = true;
                transform.position = player.hit.point - direction.normalized * 0.45f;
                player.RandomizeBloodRotationAndScale(transform);
                transform.gameObject.SetActive(value: true);
            }
            currentBloodIndex = (currentBloodIndex + 1) % playerPinkBloodPooledObjects.Count;
            if (player.isPlayerDead || snowFootprintsPooledObjects == null || snowFootprintsPooledObjects.Count <= 0)
            {
                return;
            }
            player.alternatePlaceFootprints = !player.alternatePlaceFootprints;
            if (player.alternatePlaceFootprints)
            {
                return;
            }
            Transform transform2 = snowFootprintsPooledObjects[currentFootprintIndex].transform;
            transform2.rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (!flag)
            {
                player.interactRay = new Ray(base.transform.position + base.transform.up * 0.3f, direction);
                if (Physics.Raycast(player.interactRay, out player.hit, 6f, player.playersManager.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                {
                    transform2.position = player.hit.point - direction.normalized * 0.45f;
                }
            }
            else
            {
                transform2.position = player.hit.point - direction.normalized * 0.45f;
            }
            transform2.transform.eulerAngles = new Vector3(transform2.transform.eulerAngles.x, base.transform.eulerAngles.y, transform2.transform.eulerAngles.z);
            snowFootprintsPooledObjects[currentFootprintIndex].enabled = true;
            currentFootprintIndex = (currentFootprintIndex + 1) % snowFootprintsPooledObjects.Count;
        }
    }

    [HarmonyPatch]
    internal class SCP1079Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.InstantiateFootprintsPooledObjects))]
        private static void InstantiateFootprintsPooledObjectsPostfix()
        {
            try
            {
                PinkBloodManager.InstantiateFootprintsPooledObjects();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.InstantiateBloodPooledObjects))]
        private static void InstantiateBloodPooledObjectsPostfix(PlayerControllerB __instance)
        {
            try
            {
                PinkBloodManager.Instance(__instance)?.InstantiateBloodPooledObjects();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetPooledObjects))]
        private static void ResetPooledObjectsPostfix()
        {
            try
            {
                PinkBloodManager.ResetPooledObjects();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ResetPlayerBloodObjects))]
        private static void ResetPlayerBloodObjectsPostfix(PlayerControllerB __instance, bool resetBodyBlood)
        {
            try
            {
                PinkBloodManager.Instance(__instance)?.ResetPlayerBloodObjects(resetBodyBlood);
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
                PinkBloodManager.Instance(__instance)?.AddBloodToBody(false);
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}
*/