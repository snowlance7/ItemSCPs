using Dawn.Utils;
using GameNetcodeStuff;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

// TODO: Screws up players audio/listening?

namespace ItemSCPs.SCP
{
    internal class SCP012Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public AudioSource audioSource = null!;
        public AudioSource audioSource2D = null!;
        public AudioClip[] speechSFX = null!;
        public AudioClip finalSpeechSFX = null!;
        public AudioClip[] stabSFX = null!;
        public Camera lightCamera = null!;

        int localPlayerStabAmount;

        bool localPlayerPlayingFinalSpeech;
        float timeSinceStartFinalSpeech;

        bool isLit => IsLit();
        bool heldByLocalPlayer => playerHeldBy != null && playerHeldBy == localPlayer && !isPocketed;

        float timeSinceLastSpeechStart;
        float timeSinceIntervalUpdate;

        float nextSpeechTime;

        float distance;
        float maxRange;
        float minRange;

        bool isOutside;

        bool localPlayerAffected;

        // Configs
        readonly BoundedRange speechInterval = new(10f, 15f);
        readonly BoundedRange activationRange = new(3f, 10f);
        const int speechDamage = 5;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.1f, -0.19f);
            itemProperties.rotationOffset = new Vector3(170f, 90f, 0f);
            itemProperties.floorYOffset = 90;

            itemProperties.canBeGrabbedBeforeGameStart = true;
            itemProperties.canBeInspected = true;
            itemProperties.twoHanded = false;
            lightCamera.clearFlags = CameraClearFlags.SolidColor;
            lightCamera.backgroundColor = Color.black;
            lightCamera.cullingMask = 1 << LayerMask.NameToLayer("Props");
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

            audioSource.volume = localPlayerAffected ? 1 : 0;
            audioSource.maxDistance = maxRange;

            if (!localPlayerAffected)
            {
                if (heldByLocalPlayer && localPlayer.activatingItem)
                {
                    localPlayer.activatingItem = false;
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
                VignetteOverlay.SetIntensity(1 - (localPlayer.health / 100));

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

            /*if (!hinderingLocalPlayer)
            {
                hinderingLocalPlayer = true;
                localPlayer.isMovementHindered++;
                localPlayer.hinderedMultiplier *= 2f;
                localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    hinderingLocalPlayer = false;
                    localPlayer.isMovementHindered--;
                    localPlayer.hinderedMultiplier *= 0.5f;
                }, "SCP-012", "Hindering Local Player", 10f, (existing, incoming) => StatusEffectController.ConflictResult.Replace));
            }*/

            localPlayer.drunkness = 0.3f;

            RoundManager.PlayRandomClip(audioSource2D, speechSFX);
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

            VignetteOverlay.SetIntensity(normalized / 2); // TODO: Test this

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
            if (SCP714Behavior.localPlayerAffected) { return false; }
            if (TESTING.immunity) { return false; }
            if (localPlayerPlayingFinalSpeech) { return true; }
            if (StartOfRound.Instance.inShipPhase && !Utils.inTestRoom) { return false; }
            if (playerHeldBy != null && localPlayer != playerHeldBy) { return false; }
            if (isPocketed) { return false; }
            if (heldByLocalPlayer) { return isLit; }
            if (distance > maxRange) { return false; }
            if (!isLit) { return false; }
            return true;
        }

        void PlayFinalSpeech()
        {
            localPlayerPlayingFinalSpeech = true;
            timeSinceStartFinalSpeech = 0f;

            audioSource2D.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            audioSource2D.PlayOneShot(finalSpeechSFX);
        }

        public bool IsLit()
        {
            lightCamera.Render();

            RenderTexture.active = lightCamera.targetTexture;

            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGB24, false);

            tex.ReadPixels(
                new Rect(0, 0, 32, 32),
                0,
                0);

            tex.Apply();

            Color[] pixels = tex.GetPixels();

            float totalBrightness = 0f;

            foreach (Color c in pixels)
            {
                totalBrightness += c.grayscale;
            }

            float average =
                totalBrightness / pixels.Length;

            logger.LogDebug(average);

            return average > 0.015f;
        }
    }
}