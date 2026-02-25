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
namespace ItemSCPs.SCPs.Snowy
{
    internal class SCP012Behavior : PhysicsProp // TODO: Make this work with SCP-714
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip[] speechSFX;
        public AudioClip finalSpeechSFX;
        public AudioClip[] stabSFX;
        public AnimationCurve pullCurve;
#pragma warning restore CS8618

        SCP012Vignette? vignetteLocal
        {
            get
            {
                if (!localPlayer.gameObject.TryGetComponent<SCP012Vignette>(out SCP012Vignette vignette))
                {
                    vignette = localPlayer.gameObject.AddComponent<SCP012Vignette>();
                }
                return vignette;
            }
        }

        int localPlayerStabAmount;

        bool localPlayerPlayingFinalSpeech;
        float timeSinceStartFinalSpeech;

        bool isLit => GetLightAt(transform.position, maxRange) > lightThreshold;
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

            TestingHUDOverlay.Instance?.SetToggle1("Affected", localPlayerAffected);

            audioSource.maxDistance = maxRange;
            //audioSource.volume = localPlayerAffected ? 1f : 0f;

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
            vignetteLocal?.SetIntensity(1f);

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

            // TODO: Bleed status effect here???

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
            //nextSpeechTime = speechInterval.GetRandomInRange(Utils.randomLocal);
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

            localPlayer.drunkness = 0.3f; // TODO: Test this
            localPlayer.sprintMeter = 0f;

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

            vignetteLocal?.SetIntensity(normalized);

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
            if (localPlayerPlayingFinalSpeech) { return true; }
            if (StartOfRound.Instance.inShipPhase && !Utils.inTestRoom) { return false; }
            if (playerHeldBy != null && localPlayer != playerHeldBy) { return false; }
            //if (heldByLocalPlayer) { return !isLit; } // TODO: Test this
            if (distance > maxRange) { return false; }
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
            playerVoice.volume = 1f;

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
            playerVoice.volume = 1f;
            playerVoice.PlayOneShot(finalSpeechSFX);
        }
    }

    public class SCP012Vignette : MonoBehaviour
    {
        private Image vignetteImage;
        private Canvas canvas;

        public void Start()
        {
            CreateVignette();
        }

        public void CreateVignette()
        {
            if (canvas != null) return;

            GameObject canvasObj = new GameObject("SCP012_Vignette");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // above everything

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject imageObj = new GameObject("VignetteImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            vignetteImage = imageObj.AddComponent<Image>();
            vignetteImage.color = new Color(0f, 0f, 0f, 0f);

            RectTransform rt = vignetteImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            vignetteImage.sprite = CreateRadialSprite();
        }

        private Sprite CreateRadialSprite()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.InverseLerp(maxDist * 0.5f, maxDist, dist);
                    alpha = Mathf.Clamp01(alpha);

                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }

            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f)
            );
        }
        public void SetIntensity(float normalized)
        {
            if (vignetteImage == null) return;

            float alpha = Mathf.Lerp(0f, 0.8f, normalized * normalized);
            vignetteImage.color = new Color(0f, 0f, 0f, alpha);
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