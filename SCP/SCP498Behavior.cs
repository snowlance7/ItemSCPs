using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP498Behavior : PhysicsProp, ISCP, ISingletonItem // TODO: Make this work with SCP-714 // TODO: Use ears ringing timer in soundmanager for scp498 // TODO: Set up 2D audio and fake it or set up 3d audio by doors to be more accurate, set panning depending on direction to player
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public ScanNodeProperties scanNode = null!;
        public AudioSource audioSource = null!;
        public GameObject audioSourcePrefab = null!;
        public TextMeshPro timeDisplay = null!;

        Dictionary<EntranceTeleport, AudioSource> audioSources = new Dictionary<EntranceTeleport, AudioSource>();

        float timeSinceAlarmActive => timeSinceLastSnooze - snoozeTime;

        bool alarmActive => timeSinceLastSnooze > snoozeTime;

        float volumeIncreaseMultiplier => 1 / timeToMaxVolume;

        float farthestNodeDistance;

        Vector3 lastPosition;

        float alarmIntensity;
        float playerIntensity;

        float localPlayerDistance;

        float timeSinceLastSnooze;
        float timeSinceCalculateVolumes;
        float timeSinceDoPlayerEffects = 2f;

        bool snoozing;

        float minDistance;
        float maxDistance;
        const float basePushForce = 1.5f; // TODO: Test this
        const float snoozeTime = 120f;
        const float timeToMaxVolume = 300f;
        const float minDistanceOffset = 0.5f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.15f, 0.23f, -0.3f);
            itemProperties.rotationOffset = new Vector3(80f, 90f, 0f);
            itemProperties.floorYOffset = 90;
        }

        public override void Start()
        {
            base.Start();
            Utils.OnShipLanded.AddListener(CreateAudioSources);
            if (StartOfRound.Instance.shipHasLanded)
                CreateAudioSources();
            Utils.allAINodes.GetFarthestFromPosition(transform.position, x => x.transform.position, out farthestNodeDistance, fastDistanceCheck: true);
            lastPosition = transform.position;
        }

        public override void OnDestroy()
        {
            foreach (var source in audioSources.Values)
            {
                Destroy(source.gameObject);
            }
            base.OnDestroy();
        }

        public override void Update()
        {
            base.Update();

            if (StartOfRound.Instance.inShipPhase)
            {

                if (IsServer && alarmActive)
                    SnoozeRpc();
                return;
            }

            timeSinceLastSnooze += Time.deltaTime;
            SetTimeDisplay();

            if (alarmActive)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.volume = 0f;
                    audioSource.Play();
                    foreach (var source in audioSources.Values)
                    {
                        source.volume = 0f;
                        source.Play();
                    }
                    grabbable = false;
                    grabbableToEnemies = false;
                    customGrabTooltip = "Snooze [E]";
                }

                if (IsServer && alarmIntensity >= 1 && !TimeOfDay.Instance.shipLeavingAlertCalled)
                    NetworkHandler.Instance.SetShipLeaveEarlyServerRpc(TimeOfDay.Instance.normalizedTimeOfDay + 0.1f, $"WARNING! Due to unsafe conditions, the autopilot ship will leave early. Please return by {HUDManager.Instance.GetClockTimeFormatted(TimeOfDay.Instance.normalizedTimeOfDay + 0.1f, TimeOfDay.Instance.numberOfHours, createNewLine: false)}.");

                CalculateVolumes();
                DoPlayerEffects();
                PushPlayer();
                SyncAudios();
            }
        }

        public override void InteractItem()
        {
            if (snoozing || !alarmActive) { return; }
            snoozing = true;
            IEnumerator SnoozeDelay()
            {
                yield return null;
                SnoozeRpc();
                snoozing = false;
            }
            StartCoroutine(SnoozeDelay());
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || !alarmActive) { return; }
            SnoozeRpc();
        }

        void SetTimeDisplay()
        {
            string time = HUDManager.Instance.clockNumber.text.Replace("\n", " ");
            timeDisplay.text = time;
        }

        void CalculateVolumes()
        {
            timeSinceCalculateVolumes += Time.deltaTime;
            if (timeSinceCalculateVolumes < 1f) { return; }
            timeSinceCalculateVolumes = 0f;

            alarmIntensity = Mathf.Clamp01(timeSinceAlarmActive * volumeIncreaseMultiplier);

            if (lastPosition != transform.position)
                Utils.allAINodes.GetFarthestFromPosition(transform.position, x => x.transform.position, out farthestNodeDistance, fastDistanceCheck: true);
            lastPosition = transform.position;

            if (farthestNodeDistance <= 0) { return; }

            audioSource.volume = localPlayer.isInsideFactory == isInFactory ? alarmIntensity : 0;
            audioSource.maxDistance = Mathf.Lerp(10f, farthestNodeDistance + 10f, alarmIntensity);
            audioSource.minDistance = audioSource.maxDistance * minDistanceOffset;
            minDistance = audioSource.minDistance;
            maxDistance = audioSource.maxDistance;

            if (localPlayer.isInsideFactory == isInFactory)
            {
                foreach (var source in audioSources.Values)
                    source.volume = 0f;
                return;
            }

            foreach (var entrance in Utils.entrances)
            {
                if (!entrance.isEntranceToBuilding) { continue; }

                AudioSource localSource = isInFactory
                    ? audioSources[entrance.exitScript]
                    : audioSources[entrance];

                AudioSource remoteSource = isInFactory
                    ? audioSources[entrance]
                    : audioSources[entrance.exitScript];

                localSource.volume = 0f;

                float distanceToPortal = Vector3.Distance(transform.position, localSource.transform.position);
                if (distanceToPortal > audioSource.maxDistance)
                    continue;

                float attenuation = 1f - distanceToPortal / audioSource.maxDistance;

                remoteSource.volume = (alarmIntensity / 2) * attenuation;
                remoteSource.maxDistance = audioSource.maxDistance - distanceToPortal;
                remoteSource.minDistance = remoteSource.maxDistance * minDistanceOffset;
            }
        }

        void CreateAudioSources()
        {
            logger.LogDebug("Creating audio sources for 498");
            audioSources.Clear();
            foreach (var entrance in Utils.entrances)
            {
                if (!entrance.gotExitPoint)
                {
                    if (entrance.FindExitPoint())
                        entrance.gotExitPoint = true;
                    else continue;
                }

                AudioSource audioSource1 = Instantiate(audioSourcePrefab, GetPointBehindDoor(entrance), Quaternion.identity).GetComponent<AudioSource>();
                audioSource1.gameObject.name = $"SCP498_{entrance.name}_AudioPortal";
                audioSources.Add(entrance, audioSource1);

                AudioSource audioSource2 = Instantiate(audioSourcePrefab, GetPointBehindDoor(entrance.exitScript), Quaternion.identity).GetComponent<AudioSource>();
                audioSource2.gameObject.name = $"SCP498_{entrance.exitScript.name}_AudioPortal";
                audioSources.Add(entrance.exitScript, audioSource2);
            }
        }

        Vector3 GetPointBehindDoor(EntranceTeleport entrance)
        {
            Vector3 local = entrance.transform.InverseTransformPoint(entrance.entrancePoint.position);

            local.x = -local.x;
            local.z = -local.z;

            local.y += 0.5f;

            Vector3 oppositePoint = entrance.transform.TransformPoint(local);
            return oppositePoint;
        }

        void SyncAudios()
        {
            foreach (var source in audioSources.Values)
                source.time = audioSource.time;
        }

        void DoPlayerEffects()
        {
            timeSinceDoPlayerEffects += Time.deltaTime;
            if (timeSinceDoPlayerEffects < 2f) { return; }
            timeSinceDoPlayerEffects = 0f;

            localPlayerDistance = Utils.SmartDistance(localPlayer.transform.position, transform.position, fastDistanceCheck: true);
            playerIntensity = alarmIntensity * (Mathf.Clamp01(1f - localPlayerDistance / audioSource.maxDistance));

            if (playerIntensity > 0.5f)
            {
                float drunknessSet = Mathf.Lerp(0f, 0.25f, playerIntensity);
                localPlayer.drunkness = Mathf.Max(localPlayer.drunkness, drunknessSet);
            }
            if (playerIntensity > 0.75f)
            {
                if (!HUDManager.Instance.playerScreenShakeAnimator.GetBool("ShakingConstant"))
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Constant);
            }
            if (playerIntensity > 0.85f)
            {
                int damageAmount = playerIntensity > 0.95f ? 2 : 1;
                localPlayer.inSpecialInteractAnimation = true;
                localPlayer.DamagePlayer(damageAmount, hasDamageSFX: false);
                localPlayer.inSpecialInteractAnimation = false;

                float vignetteIntensity = Mathf.Lerp(0f, 0.4f, playerIntensity);
                VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, vignetteIntensity));
            }
        }

        void PushPlayer() // TODO: ADJUST THIS, CURRENTLY SENDS THE PLAYER FLYING
        {
            if (playerIntensity < 0.8f) { return; }
            float pushForce = basePushForce * playerIntensity;
            float pushDistance = audioSource.minDistance / 2;

            if (localPlayerDistance > pushDistance) { return; }

            Vector3 pushDirection = (localPlayer.playerCollider.transform.position - transform.position).normalized;
            Vector3 targetPosition = localPlayer.playerCollider.transform.position + (pushDirection * pushForce);

            float pushForceMultiplier = 1 - (localPlayerDistance / pushDistance);
            localPlayer.playerCollider.transform.position = Vector3.Lerp(localPlayer.playerCollider.transform.position, targetPosition, pushForce * pushForceMultiplier * Time.fixedDeltaTime);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SnoozeRpc()
        {
            foreach (var source in audioSources.Values)
            {
                source.volume = 0f;
                source.Stop();
            }
            audioSource.volume = 0f;
            audioSource.Stop();
            timeSinceLastSnooze = 0f;
            grabbable = true;
            grabbableToEnemies = true;
            customGrabTooltip = "";

            if (HUDManager.Instance.playerScreenShakeAnimator.GetBool("ShakingConstant"))
                HUDManager.Instance.StopShakingCamera();

            SoundManager.Instance.earsRingingTimer = 5 * playerIntensity;
        }
    }
}