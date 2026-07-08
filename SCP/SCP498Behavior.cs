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

        float alarmIntensity;

        float timeSinceLastSnooze;
        float timeSinceCalculateMaxDistance;

        bool snoozing;

        const float snoozeTime = 120f;
        const float timeToMaxVolume = 300f;
        const float maxDistanceOffset = 10f;

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

                timeSinceCalculateMaxDistance += Time.deltaTime;
                if (timeSinceCalculateMaxDistance > 1f)
                {
                    timeSinceCalculateMaxDistance = 0f;
                    CalculateVolumes();
                    SyncAudios();
                    DoPlayerEffects();
                }
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
            if (!buttonDown) { return; }
            SnoozeRpc();
        }

        void SetTimeDisplay()
        {
            string time = HUDManager.Instance.clockNumber.text.Replace("\n", " ");
            timeDisplay.text = time;
        }

        void CalculateVolumes()
        {
            alarmIntensity = Mathf.Clamp01(timeSinceAlarmActive * volumeIncreaseMultiplier);

            Utils.allAINodes.GetFarthestFromPosition(
                transform.position,
                x => x.transform.position,
                out float farthestDistance,
                fastDistanceCheck: true);

            audioSource.volume = localPlayer.isInsideFactory == isInFactory ? alarmIntensity : 0;
            audioSource.maxDistance = Mathf.Lerp(10f, farthestDistance + 10f, alarmIntensity);

            if (localPlayer.isInsideFactory == isInFactory)
            {
                foreach (var source in audioSources.Values)
                    source.volume = 0f;
                return;
            }

            foreach (var entrance in Utils.entrances)
            {
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

                remoteSource.maxDistance = audioSource.maxDistance - distanceToPortal;
                remoteSource.volume = (alarmIntensity / 3) * attenuation;
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
                audioSource1.gameObject.name = $"SCP498_{entrance.name}_TempAudioSource";
                audioSources.Add(entrance, audioSource1);

                AudioSource audioSource2 = Instantiate(audioSourcePrefab, GetPointBehindDoor(entrance.exitScript), Quaternion.identity).GetComponent<AudioSource>();
                audioSource2.gameObject.name = $"SCP498_{entrance.exitScript.name}_TempAudioSource";
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
            var distanceToAlarm = Utils.SmartDistance(localPlayer.transform.position, transform.position, fastDistanceCheck: true);
            var playerIntensity = 
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
        }
    }
}