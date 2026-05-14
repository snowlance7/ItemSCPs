using SnowyLib;
using System.Collections.Generic;
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
        public TextMeshPro timeDisplay = null!;

        float timeSinceAlarmActive => timeSinceLastSnooze - snoozeTime;

        bool alarmActive => timeSinceLastSnooze > snoozeTime;

        float volumeIncreaseMultiplier => 1 / timeToMaxVolume;

        float timeSinceLastSnooze;


        const float snoozeTime = 120f;
        const float timeToMaxVolume = 300f;

        public override void Update()
        {
            base.Update();
            if (!StartOfRound.Instance.inShipPhase)
            {
                timeSinceLastSnooze += Time.deltaTime;
                SetTimeDisplay();
            }

            if (alarmActive && !audioSource.isPlaying)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                    grabbable = false;
                    grabbableToEnemies = false;
                    customGrabTooltip = "Snooze [E]";
                }

                audioSource.volume = timeSinceAlarmActive * volumeIncreaseMultiplier;
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

        void CalculateMaxDistance()
        {
            if (localPlayer.isInsideFactory == isInFactory)
            {
                GameObject farthestNode = Utils.insideAINodes.GetFarthestFromPosition(transform.position, (x) => x.transform.position)!;
                float maxDistance = Vector3.Distance(transform.position, farthestNode.transform.position) + 10;
                audioSource.maxDistance = Mathf.Lerp(10f, maxDistance, audioSource.volume);
            }
            else
            {
                foreach (var entrance in Utils.entrances)
                {
                    // TODO
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