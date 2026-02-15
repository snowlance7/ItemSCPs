using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;
// TODO: Make config for making all the scp items names to be generic names instead of the SCP-??? when you scan them? Make it default?
namespace ItemSCPs.SCPs.Snowy
{
    internal class SCP012Behavior : PhysicsProp // TODO: Make this work with SCP-714
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip[] speechSFX;
        public AudioClip finalSpeechSFX;
        public AudioClip[] stabSFX;
#pragma warning restore CS8618

        int localPlayerStabAmount;

        bool localPlayerPlayingFinalSpeech;
        float timeSinceStartFinalSpeech;

        bool isLit => GetLightAt(transform.position, range) > lightThreshold;
        bool heldByLocalPlayer => playerHeldBy != null && playerHeldBy == localPlayer && !isPocketed;
        AudioSource? playerVoice => playerHeldBy?.itemAudio;

        float timeSinceLastSpeech;
        float timeSinceIntervalUpdate;

        float nextSpeechTime;

        float distance;
        float range;

        static Light[] lights = [];

        bool isOutside;

        // Configs
        public static bool configEnabled => ItemSCPsContentHandler.Instance.SCP012 != null; // TODO: Test this
        readonly BoundedRange speechInterval = new BoundedRange(10f, 15f);
        const float activationRange = 10f;
        const float forceLookIntensity = 0.25f;
        const float forceWalkIntensity = 1f;
        const float pickupRange = 1f;
        const float lightThreshold = 0.4f;
        const int speechDamage = 5;

        public override void Start()
        {
            base.Start();

            itemProperties.positionOffset = new Vector3(0f, 0.1f, -0.19f);
            itemProperties.rotationOffset = new Vector3(170f, 90f, 0f);
            itemProperties.floorYOffset = 90;

            GetLights();
            Utils.OnFinishGeneratingLevel.AddListener(GetLights);
        }

        public override void Update()
        {
            base.Update();

            timeSinceStartFinalSpeech += Time.deltaTime;
            timeSinceLastSpeech += Time.deltaTime;
            timeSinceIntervalUpdate += Time.deltaTime;

            if (playerHeldBy != null)
            {
                isOutside = !playerHeldBy.isInsideFactory;
            }

            if (timeSinceIntervalUpdate > 0.2f)
            {
                timeSinceIntervalUpdate = 0f;
                IntervalUpdate();
            }
        }

        void IntervalUpdate()
        {
            if (!localPlayer.criticallyInjured) { localPlayerStabAmount = 0; }

            range = activationRange;

            if (isOutside && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
            {
                range = activationRange / 2;
            }

            audioSource.maxDistance = range;

            bool affected = CanAffectPlayer();
            TestingHUDOverlay.Instance?.SetToggle1("Affected", affected);
            audioSource.volume = affected ? 1f : 0f;
            if (!affected)
            {
                if (heldByLocalPlayer && localPlayer.activatingItem)
                {
                    localPlayer.activatingItem = false;
                    MufflePlayerServerRpc(localPlayer.actualClientId, false);
                }
                localPlayerPlayingFinalSpeech = false;
                return;
            }

            if (!heldByLocalPlayer)
            {
                if (distance < pickupRange)
                {
                    Utils.TryForceLocalPlayerGrabItem(this, makeRoomToGrab: true);
                    return;
                }

                localPlayer.turnCompass.LookAt(transform);
                localPlayer.transform.rotation = Quaternion.Lerp(localPlayer.transform.rotation, localPlayer.turnCompass.rotation, forceLookIntensity * Time.deltaTime);

                MovePlayerTowardsPosition(transform.position, forceWalkIntensity);
                return;
            }

            localPlayer.activatingItem = true;

            if (localPlayerPlayingFinalSpeech)
            {
                if (timeSinceStartFinalSpeech > finalSpeechSFX.length)
                {
                    RoundManager.PlayRandomClip(audioSource, stabSFX);
                    localPlayer.KillPlayer(Vector3.zero, causeOfDeath: CauseOfDeath.Stabbing);
                    localPlayer.activatingItem = false;
                    // TODO: ???
                }
                return;
            }

            // TODO: Bleed status effect here???

            if (timeSinceLastSpeech > nextSpeechTime)
            {
                timeSinceLastSpeech = 0f;
                nextSpeechTime = speechInterval.GetRandomInRange(Utils.randomLocal);
                DamageSelf();
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            timeSinceLastSpeech = 0f;
            nextSpeechTime = speechInterval.GetRandomInRange(Utils.randomLocal);
            localPlayer.activatingItem = true;
        }

        void DamageSelf()
        {
            localPlayerStabAmount++;
            int damage = localPlayerStabAmount * speechDamage;

            if (localPlayer.health - damage <= 0)
            {
                PlayFinalSpeech();
                return;
            }

            RoundManager.PlayRandomClip(audioSource, stabSFX);
            localPlayer.inSpecialInteractAnimation = true;
            localPlayer.DamagePlayer(damage, causeOfDeath: CauseOfDeath.Stabbing);
            localPlayer.inSpecialInteractAnimation = false;
            PlaySpeechServerRpc();
        }

        void MovePlayerTowardsPosition(Vector3 targetPosition, float force)
        {
            Vector3 direction = (targetPosition - localPlayer.playerCollider.transform.position).normalized;
            float step = force * Time.fixedDeltaTime;

            if (Vector3.Distance(localPlayer.playerCollider.transform.position, targetPosition) > step)
            {
                localPlayer.playerCollider.transform.position += direction * step;
            }
            else
            {
                localPlayer.playerCollider.transform.position = targetPosition;
            }
        }

        bool CanAffectPlayer()
        {
            if (localPlayerPlayingFinalSpeech) { return true; }
            if (StartOfRound.Instance.inShipPhase && !Utils.inTestRoom) { return false; }
            if (playerHeldBy != null && localPlayer != playerHeldBy) { return false; }
            //if (heldByLocalPlayer) { return !isLit; } // TODO: Test this
            distance = Vector3.Distance(transform.position, localPlayer.transform.position);
            if (distance > range) { return false; }
            //if (!isLit) { return false; }
            return true;
        }

        float GetLightAt(Vector3 pos, float radius)
        {
            float total = 0f;

            foreach (var col in Physics.OverlapSphere(pos, radius))
            {
                Light l = col.GetComponent<Light>();
                if (!l || !l.enabled) continue;

                float dist = Vector3.Distance(pos, l.transform.position);
                if (dist > l.range) continue;

                // optional occlusion
                if (Physics.Raycast(l.transform.position, pos - l.transform.position, dist))
                    continue;

                float atten = 1f - (dist / l.range);
                total += l.intensity * atten;
            }

            return total;
        }

        void PlayFinalSpeech()
        {
            localPlayerPlayingFinalSpeech = true;
            timeSinceStartFinalSpeech = 0f;
            PlayFinalSpeechServerRpc();
        }

        public static void GetLights()
        {
            lights = FindObjectsOfType<Light>();
        }

        [ServerRpc(RequireOwnership = false)]
        public void MufflePlayerServerRpc(ulong clientId, bool value)
        {
            if (!IsServer) { return; }
            MufflePlayerClientRpc(clientId, value);
        }

        [ClientRpc]
        public void MufflePlayerClientRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            Utils.MufflePlayer(player, value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlaySpeechServerRpc()
        {
            if (!IsServer) { return; }
            PlaySpeechClientRpc();
        }

        [ClientRpc]
        public void PlaySpeechClientRpc()
        {
            if (playerVoice == null) { return; }
            Utils.MufflePlayer(playerHeldBy, true);
            playerVoice.pitch = Random.Range(0.94f, 1.06f);

            int index = Utils.randomLocal.Next(0, speechSFX.Length);
            playerVoice.PlayOneShot(speechSFX[index]);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayFinalSpeechServerRpc()
        {
            if (!IsServer) { return; }
            PlayFinalSpeechClientRpc();
        }

        [ClientRpc]
        public void PlayFinalSpeechClientRpc()
        {
            if (playerVoice == null) { logger.LogWarning("PlayerVoice is null"); return; }
            playerVoice.pitch = Random.Range(0.94f, 1.06f);
            playerVoice.PlayOneShot(finalSpeechSFX);
        }
    }

    [HarmonyPatch]
    internal class SCP012Patches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingLevel))]
        internal static void FinishGeneratingLevelPostfix()
        {
            try
            {
                if (SCP012Behavior.configEnabled)
                {
                    SCP012Behavior.GetLights();
                }
            }
            catch
            {
                return;
            }
        }
    }
}