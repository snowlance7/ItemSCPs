using Dawn.Utils;
using DunGen;
using GameNetcodeStuff;
using HarmonyLib;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static ItemSCPs.Plugin;
// TODO: Make config for making all the scp items names to be generic names instead of the SCP-??? when you scan them? Make it default?
namespace ItemSCPs.SCP
{
    internal class SCP012Behavior : PhysicsProp // TODO: // Set up light functionality
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

        bool isLit => GetLightAt(transform.position, maxRange) > lightThreshold; // TODO: Test and make sure this works
        bool heldByLocalPlayer => playerHeldBy != null && playerHeldBy == localPlayer && !isPocketed;
        AudioSource? playerVoice => playerHeldBy?.itemAudio;

        float timeSinceLastSpeechStart;
        float timeSinceIntervalUpdate;

        float nextSpeechTime;

        float distance;
        float maxRange;
        float minRange;

        static Light[] lights = [];

        bool isOutside;

        bool localPlayerAffected;

        // Configs
        public static bool configEnabled => ItemSCPsContentHandler.Instance.SCP012 != null; // TODO: Test this
        readonly BoundedRange speechInterval = new(10f, 15f);
        readonly BoundedRange activationRange = new(3f, 10f);
        const float lightThreshold = 0.4f;
        const int speechDamage = 5;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.1f, -0.19f);
            itemProperties.rotationOffset = new Vector3(170f, 90f, 0f);
            itemProperties.floorYOffset = 90;

            itemProperties.canBeGrabbedBeforeGameStart = true;
            itemProperties.canBeInspected = true;
            //itemProperties.twoHanded = true;
        }

        public override void Start()
        {
            base.Start();

            GetLights();
            Utils.OnFinishGeneratingLevel.AddListener(GetLights);
        }

        public override void Update()
        {
            base.Update();

            timeSinceStartFinalSpeech += Time.deltaTime;
            timeSinceLastSpeechStart += Time.deltaTime;
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

            if (localPlayerAffected && playerHeldBy == null)
                ForcePlayerMovementUpdate();
        }

        void IntervalUpdate()
        {
            distance = Vector3.Distance(transform.position, localPlayer.transform.position);
            if (!localPlayer.criticallyInjured) { localPlayerStabAmount = 0; }

            bool foggy = isOutside && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy;
            maxRange = foggy ? activationRange.Max / 2 : activationRange.Max;
            minRange = foggy ? activationRange.Min / 2 : activationRange.Min;

            localPlayerAffected = CanAffectPlayer();

            audioSource.maxDistance = maxRange;

            if (!localPlayerAffected)
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
                if (distance <= 1f && !localPlayer.isGrabbingObjectAnimation && !localPlayer.isTypingChat && !localPlayer.inTerminalMenu && !localPlayer.throwingObject && !localPlayer.IsInspectingItem && !(localPlayer.inAnimationWithEnemy != null) && !localPlayer.jetpackControls && !localPlayer.disablingJetpackControls && !StartOfRound.Instance.suckingPlayersOutOfShip && !localPlayer.activatingItem && !localPlayer.waitingToDropItem)
                {
                    if (IsInventoryFull(localPlayer)) { localPlayer.DiscardHeldObject(); }
                    localPlayer.BeginGrabObject();
                }
                return;
            }

            localPlayer.activatingItem = true;
            localPlayer.sprintMeter = 0f;
            localPlayer.isExhausted = true;
            if (localPlayer.health > 0)
                StatusEffectController.Instance.vignetteOverlay?.SetIntensity(1 - (100 / localPlayer.health));

            if (localPlayerPlayingFinalSpeech)
            {
                if (timeSinceStartFinalSpeech > finalSpeechSFX.length)
                {
                    RoundManager.PlayRandomClip(audioSource, stabSFX);
                    localPlayer.KillPlayer(Vector3.zero, causeOfDeath: CauseOfDeath.Stabbing);
                    localPlayer.activatingItem = false;
                }
                return;
            }

            if (timeSinceLastSpeechStart > nextSpeechTime)
            {
                timeSinceLastSpeechStart = 0f;
                nextSpeechTime = speechInterval.GetRandomInRange(Utils.randomLocal);
                DamageSelf();
            }
        }

        bool IsInventoryFull(PlayerControllerB player)
        {
            foreach (var slot in player.ItemSlots)
            {
                if (slot == null) { return false; }
            }
            return true;
        }

        public override void EquipItem()
        {
            base.EquipItem();
            timeSinceLastSpeechStart = 0f;
            nextSpeechTime = 3f;
            localPlayer.activatingItem = CanAffectPlayer();
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
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);

            if (!localPlayer.criticallyInjured)
                localPlayer.MakeCriticallyInjured(true);

            // TODO: Hinder movement?

            localPlayer.drunkness = 0.3f;

            PlaySpeechServerRpc();
        }

        void MovePlayerTowardsPosition(Vector3 targetPosition, float force)
        {
            if (distance <= 1f) { return; }
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

        void ForcePlayerMovementUpdate()
        {
            float normalized = Mathf.InverseLerp(maxRange, minRange, distance);
            float pullStrength = normalized * normalized;

            StatusEffectController.Instance.vignetteOverlay?.SetIntensity(normalized);

            MovePlayerTowardsPosition(transform.position, normalized);

            float dt = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);

            // ----- YAW -----

            Vector3 flatDir = transform.position - localPlayer.thisPlayerBody.position;
            flatDir.y = 0f;

            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetYaw = Quaternion.LookRotation(flatDir.normalized);

                localPlayer.thisPlayerBody.rotation = Quaternion.Slerp(
                    localPlayer.thisPlayerBody.rotation,
                    targetYaw,
                    pullStrength * dt
                );
            }

            // ----- PITCH -----

            Vector3 dir = (transform.position - localPlayer.gameplayCamera.transform.position).normalized;
            float targetPitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;

            localPlayer.cameraUp = Mathf.Lerp(
                localPlayer.cameraUp,
                targetPitch,
                pullStrength * dt
            );

            localPlayer.cameraUp = Mathf.Clamp(localPlayer.cameraUp, -89f, 89f);

            localPlayer.gameplayCamera.transform.localEulerAngles =
                new Vector3(
                    localPlayer.cameraUp,
                    localPlayer.gameplayCamera.transform.localEulerAngles.y,
                    0f
                );
        }

        bool CanAffectPlayer()
        {
            if (Utils.isBeta && Utils.testing)
            {
                return !TESTING.localPlayerImmune;
            }
            if (SCP714Behavior.localPlayerAffected) { return false; }
            if (localPlayerPlayingFinalSpeech) { return true; }
            if (StartOfRound.Instance.inShipPhase && !Utils.inTestRoom) { return false; }
            if (playerHeldBy != null && localPlayer != playerHeldBy) { return false; }
            if (heldByLocalPlayer) { return !isLit; } // TODO: Test this
            if (distance > maxRange) { return false; }
            if (!isLit) { return false; } // TODO: Test this
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

                float atten = 1f - dist / l.range;
                total += l.intensity * atten;
            }

            TestingHUDOverlay.Instance?.SetLabel1("Light level SCP012: " + total); // TODO: Test
            return total;
        }

        float GetLightLevelAtPosition(Vector3 pos)
        {
            float brightness = 0f;

            foreach (Light light in lights)
            {
                float dist = Vector3.Distance(light.transform.position, pos);

                if (dist < light.range)
                {
                    float contribution = light.intensity * (1f - dist / light.range);
                    brightness += contribution;
                }
            }

            TestingHUDOverlay.Instance?.SetLabel1("Light level SCP012: " + brightness); // TODO: Test
            return brightness;
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
            playerVoice.volume = 1f;

            int index = Utils.randomGlobal.Next(0, speechSFX.Length);
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
            playerVoice.volume = 1f;
            playerVoice.PlayOneShot(finalSpeechSFX);
        }
    }
}