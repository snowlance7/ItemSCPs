using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP498Behavior : PhysicsProp // TODO: Make this work with SCP-714 // TODO: Use ears ringing timer in soundmanager for scp498
    {
        public ScanNodeProperties scanNode = null!;
        public AudioSource audioSource = null!;
        public AudioSource audioSource2D = null!;
        public TextMeshPro timeDisplay = null!;

        float timeSinceAlarmActive => timeSinceLastSnooze - snoozeTime;

        bool alarmActive => timeSinceLastSnooze > snoozeTime;

        float volumeIncreaseMultiplier => 1 / timeToMaxVolume;

        float timeSinceLastSnooze;
        float timeSinceCalculateMaxDistance;

        const float snoozeTime = 120f;
        const float timeToMaxVolume = 300f;

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

            if (alarmActive/* && !audioSource.isPlaying*/)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                    grabbable = false;
                    grabbableToEnemies = false;
                    customGrabTooltip = "Snooze [E]";
                }

                audioSource.volume = Mathf.Clamp01(timeSinceAlarmActive * volumeIncreaseMultiplier);

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
            base.InteractItem();
            SnoozeServerRpc();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            SnoozeServerRpc();
        }

        void SetTimeDisplay()
        {
            string time = HUDManager.Instance.clockNumber.text.Replace("\n", " ");
            timeDisplay.text = time;
        }

        void CalculateVolume()// TODO
        {
            GameObject[] nodes = isInFactory ? Utils.insideAINodes : Utils.outsideAINodes;
            GameObject? farthestNode = nodes.GetFarthestFromPosition(transform.position, (x) => x.transform.position);
            if (farthestNode == null) { return; }

            float maxDistance = Vector3.Distance(transform.position, farthestNode.transform.position) + 10;
            audioSource.maxDistance = Mathf.Lerp(10f, maxDistance, audioSource.volume);

            if (localPlayer.isInsideFactory == isInFactory)
            {
                audioSource2D.volume = 0f;
            }
            else
            {
                float maxVolume = 0f;
                foreach (var entrance in Utils.entrances)
                {
                    if (entrance.isEntranceToBuilding == isInFactory) { continue; }
                    if (entrance.exitScript == null && (entrance.exitPointDoesntExist || !entrance.FindExitPoint())) { continue; }

                    float alarmToEntrance = Vector3.Distance(transform.position, entrance.transform.position);
                    if (alarmToEntrance > audioSource.maxDistance) { continue; }
                    float exitToPlayer = Vector3.Distance(entrance.exitScript!.transform.position, localPlayer.transform.position); // TODO
                    GameObject[] nodes2 = isInFactory ? Utils.outsideAINodes : Utils.insideAINodes; 
                    GameObject? farthestNode2 = nodes2.GetFarthestFromPosition(transform.position, (x) => x.transform.position);
                    if (farthestNode2 == null) { continue; }

                    float maxDistance2 = Vector3.Distance(transform.position, farthestNode2.transform.position) + 10;
                    audioSource.maxDistance = Mathf.Lerp(10f, maxDistance, audioSource.volume);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SnoozeServerRpc()
        {
            if (!IsServer) { return; }
            SnoozeClientRpc();
        }

        [ClientRpc]
        public void SnoozeClientRpc()
        {
            audioSource.Stop();
            timeSinceLastSnooze = 0f;
            grabbable = true;
            grabbableToEnemies = true;
            customGrabTooltip = "";
        }
    }
}