using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
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
        public AudioSource audioSource2D = null!;
        public TextMeshPro timeDisplay = null!;

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
                    audioSource2D.volume = 0f;
                    audioSource.Play();
                    audioSource2D.Play();
                    grabbable = false;
                    grabbableToEnemies = false;
                    customGrabTooltip = "Snooze [E]";
                }

                timeSinceCalculateMaxDistance += Time.deltaTime;
                if (timeSinceCalculateMaxDistance > 1f)
                {
                    timeSinceCalculateMaxDistance = 0f;
                    CalculateVolume();
                }
            }
        }

        public override void InteractItem()
        {
            snoozing = true;
            SnoozeRpc();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown || snoozing) { snoozing = false; return; }
            SnoozeRpc();
        }

        void SetTimeDisplay()
        {
            string time = HUDManager.Instance.clockNumber.text.Replace("\n", " ");
            timeDisplay.text = time;
        }

        void CalculateVolume() // TODO: Test this
        {
            audioSource.volume = Mathf.Clamp01(timeSinceAlarmActive * volumeIncreaseMultiplier);
            GameObject[] nodes = isInFactory ? Utils.insideAINodes : Utils.outsideAINodes;
            GameObject? farthestNode = nodes.GetFarthestFromPosition(transform.position, (x) => x.transform.position, out float farthestNodeDistance);
            if (farthestNode == null) { return; }

            //logger.LogDebug(farthestNodeDistance);
            float maxDistance = farthestNodeDistance + maxDistanceOffset;
            audioSource.maxDistance = Mathf.Lerp(10f, maxDistance, audioSource.volume);

            if (localPlayer.isInsideFactory == isInFactory)
            {
                audioSource2D.volume = 0f;
                //logger.LogDebug($"Volume: {audioSource.volume} Distance: {audioSource.maxDistance}");
            }
            else
            {
                audioSource.volume = 0f;

                float maxVolume = 0f;
                foreach (var entrance in Utils.entrances)
                {
                    if (entrance.isEntranceToBuilding == isInFactory) { continue; }
                    if (entrance.exitScript == null && (entrance.exitPointDoesntExist || !entrance.FindExitPoint())) { continue; }
                    if (entrance.exitScript == null) { continue; }

                    float alarmToEntranceDistance = Vector3.Distance(transform.position, entrance.transform.position);
                    if (alarmToEntranceDistance > audioSource.maxDistance) { continue; }
                    float exitToPlayerDistance = Vector3.Distance(entrance.exitScript.transform.position, localPlayer.transform.position); // TODO
                    float totalDistanceToPlayer = alarmToEntranceDistance + exitToPlayerDistance;
                    GameObject[] nodes2 = isInFactory ? Utils.outsideAINodes : Utils.insideAINodes; 
                    GameObject? farthestNode2 = nodes2.GetFarthestFromPosition(entrance.exitScript.transform.position, (x) => x.transform.position, out float farthestNode2Distance);
                    if (farthestNode2 == null) { continue; }

                    float maxDistance2 = alarmToEntranceDistance + farthestNode2Distance + maxDistanceOffset;
                    float volume = totalDistanceToPlayer / maxDistance2;
                    if (volume < maxVolume) { continue; }
                    maxVolume = volume;
                }

                audioSource2D.volume = maxVolume; // TODO: Test this
                //logger.LogDebug($"Volume: {audioSource2D.volume}");
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SnoozeRpc()
        {
            audioSource.Stop();
            audioSource2D.Stop();
            timeSinceLastSnooze = 0f;
            grabbable = true;
            grabbableToEnemies = true;
            customGrabTooltip = "";
        }
    }
}