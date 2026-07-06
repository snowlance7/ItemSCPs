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
                if (audioSource.isPlaying)
                    audioSource.Stop();
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
                    grabbable = false;
                    grabbableToEnemies = false;
                    customGrabTooltip = "Snooze [E]";
                }

                timeSinceCalculateMaxDistance += Time.deltaTime;
                if (timeSinceCalculateMaxDistance > 1f)
                {
                    timeSinceCalculateMaxDistance = 0f;
                    CalculateVolumes();
                }
            }
        }

        public override void InteractItem()
        {
            if (snoozing || !alarmActive) { return; }
            snoozing = true;
            IEnumerator SnoozeDelay()
            {
                yield return new WaitForSeconds(1f);
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

        /*void CalculateMaxDistance()
        {
            Vector3 origin = transform.position;

            GameObject farthestNode = Utils.insideAINodes.GetFarthestFromPosition(origin, x => x.transform.position)!;

            float maxDistance = Vector3.Distance(origin, farthestNode.transform.position) + 10f;

            audioSource.maxDistance = maxDistance;

            foreach (var src in audioSources)
            {
                var entrance = src.Key;
                var audio = src.Value;

                if (isInFactory == entrance.isEntranceToBuilding)
                    continue;

                float dist = Vector3.Distance(origin, entrance.transform.position);

                if (dist > maxDistance)
                {
                    audio.volume = 0f;
                    continue;
                }

                if (entrance.exitScript == null ||
                    (entrance.exitPointDoesntExist || !entrance.FindExitPoint()))
                {
                    continue;
                }

                var exitSource = audioSources.FirstOrDefault(x => x.entrance == entrance.exitScript);
                if (exitSource == null)
                    continue;

                float t = 1f - (dist / maxDistance);

                float originalVolume = audio.volume;

                exitSource.audioSource.maxDistance = maxDistance - dist;
                exitSource.audioSource.volume = originalVolume * t;
            }
        }*/

        void CalculateVolumes() // TODO
        {
            Vector3 origin = transform.position;

            var farthestNode = Utils.allAINodes.GetFarthestFromPosition(origin, (x) => x.transform.position, out float farthestDistance, fastDistanceCheck: true);
            farthestDistance += 10f;

            audioSource.maxDistance = farthestDistance;



            foreach (var entrance in Utils.entrances)
            {
                var insideSource = audioSources[entrance.exitScript];
                var outsideSource = audioSources[entrance];

                if (isInFactory)
                {
                    insideSource.volume = 0f;


                }
                else
                {

                }
            }
        }

        void CreateAudioSources()
        {
            audioSources.Clear();
            foreach (var entrance in Utils.entrances)
            {
                if (!entrance.gotExitPoint)
                {
                    if (entrance.FindExitPoint())
                        entrance.gotExitPoint = true;
                    else continue;
                }

                AudioSource audioSource1 = Instantiate(audioSourcePrefab, entrance.transform.position, Quaternion.identity).GetComponent<AudioSource>();
                audioSource1.gameObject.name = $"SCP498_{entrance.name}_TempAudioSource";
                audioSources.Add(entrance, audioSource1);

                AudioSource audioSource2 = Instantiate(audioSourcePrefab, entrance.exitScript.transform.position, Quaternion.identity).GetComponent<AudioSource>();
                audioSource2.gameObject.name = $"SCP498_{entrance.exitScript.name}_TempAudioSource";
                audioSources.Add(entrance.exitScript, audioSource2);
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SnoozeRpc()
        {
            audioSource.Stop();
            timeSinceLastSnooze = 0f;
            grabbable = true;
            grabbableToEnemies = true;
            customGrabTooltip = "";
        }
    }
}