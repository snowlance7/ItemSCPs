using BepInEx.Logging;
using DunGen;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;
// TODO: Make this item increase time in day x2?
namespace ItemSCPs.Items.Snowy
{
    internal class SCP498Behavior : PhysicsProp // TODO: Make this work with SCP-714
    {
        public ScanNodeProperties ScanNode;

        public TextMeshPro TimeDisplay;

        string grabTooltip = "Snooze [E]";

        float timeBetweenAlarms;
        float volumeIncreaseInterval;
        float volumeIncreaseAmount;

        float timeBeforeAlarmStart;
        float timeAlarmCountdown = 0f;
        float timeAlarmActive = 0f;
        float timeSinceDamagePlayers = 0f;
        float timeSinceDelayedUpdate = 0f;

        float StartingAlarmVolume;
        float VolumeToStartDamagePlayers;
        int maxDamage;

        public AudioSource ItemSFX;

        public List<AudioClip> AlarmSounds;
        // TODO: Use ears ringing timer in soundmanager for scp498

        enum AudioClips
        {
            ExtraLoud,
            FourBeeps,
            Alien,
            Annoying
        }

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 498 behavior.");
            timeBeforeAlarmStart = ConfigManager.configTimeBeforeAlarmStart.Value;
            timeBetweenAlarms = ConfigManager.configTimeBetweenAlarms.Value;
            volumeIncreaseInterval = ConfigManager.configTimeBeforeVolumeIncrease.Value;
            volumeIncreaseAmount = ConfigManager.configVolumeIncreaseAmount.Value;
            StartingAlarmVolume = ConfigManager.configStartingAlarmVolume.Value;
            VolumeToStartDamagePlayers = ConfigManager.configVolumeToStartDamagePlayers.Value;
            maxDamage = ConfigManager.config498MaxDamage.Value;

            ItemSFX.enabled = true;
            ItemSFX.volume = StartingAlarmVolume;
            ItemSFX.clip = AlarmSounds[ConfigManager.configAlarmType.Value];
            customGrabTooltip = " ";

            ItemSFX.minDistance = ConfigManager.configMaxVolumeMinDistance.Value;
            ItemSFX.maxDistance = GetFarthestAINodeDistance();
        }

        public override void Update()
        {
            base.Update();

            SetTimeDisplay();

            if (timeBeforeAlarmStart > 0)
            {
                timeBeforeAlarmStart -= Time.deltaTime;
                //logger.LogDebug("Time left before start: " + timeBeforeAlarmStart);
                ScanNode.subText = $"Time left: {timeBeforeAlarmStart}";
                return;
            }

            if (timeAlarmCountdown > 0)
            {
                timeAlarmCountdown -= Time.deltaTime;
                //logger.LogDebug("Time left: " + timeAlarmCountdown);
                ScanNode.subText = $"Time left: {(int)timeAlarmCountdown}";
                return;
            }

            timeAlarmActive += Time.deltaTime;
            if (customGrabTooltip != grabTooltip) { customGrabTooltip = grabTooltip; }
            ScanNode.subText = $"Volume: {ItemSFX.volume}";

            timeSinceDamagePlayers += Time.deltaTime;

            timeSinceDelayedUpdate += Time.deltaTime;
            if (timeSinceDelayedUpdate > 0.22f)
            {
                timeSinceDelayedUpdate = 0f;
                DoDelayedUpdate();
            }

            if (!ItemSFX.isPlaying) { ItemSFX.Play(); }

            int volumeMultiplier = (int)(timeAlarmActive / volumeIncreaseInterval);
            float volume = volumeIncreaseAmount * volumeMultiplier;
            ItemSFX.volume = Mathf.Clamp(volume, 0f, 1f);

            if (ItemSFX.volume == 1f)
            {
                StartOfRound.Instance.ShipLeave();
            }
        }

        public void DoDelayedUpdate()
        {
            if (timeAlarmActive >= VolumeToStartDamagePlayers)
            {
                DamagePlayers();
            }
        }

        public override void InteractItem() // TODO: Make sure this works on the network
        {
            base.InteractItem();
            //logger.LogDebug("Time alarm active: " + timeAlarmActive);
            if (timeAlarmActive <= 0f) { return; }
            logger.LogDebug("Snoozing alarm.");
            timeAlarmActive = 0f;
            timeAlarmCountdown = timeBetweenAlarms;
            ItemSFX.Stop();
            ItemSFX.volume = StartingAlarmVolume;
            customGrabTooltip = " ";
        }

        public float GetFarthestAINodeDistance()
        {
            float maxDistance = 0f;

            foreach (var node in RoundManager.Instance.insideAINodes)
            {
                if (node == null) { continue; }
                float distance = Vector3.Distance(node.transform.position, transform.position);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            logger.LogDebug($"Max distance for SCP-498: {maxDistance}");
            return maxDistance;
        }

        private void SetTimeDisplay()
        {
            string time = HUDManager.Instance.clockNumber.text.Replace("\n", " ");
            TimeDisplay.text = time;
        }

        public void DamagePlayers()
        {
            if (timeSinceDamagePlayers > 3f && ItemSFX.volume >= VolumeToStartDamagePlayers / 2f)
            {
                timeSinceDamagePlayers = 0f;

                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (!player.isPlayerControlled) { continue; }
                    float distance = Vector3.Distance(transform.position, player.transform.position);

                    // MAIN Equation
                    float damageFactor = 1f - distance / ItemSFX.maxDistance; // TODO: Test this
                    int damage = (int)(maxDamage * damageFactor * ItemSFX.volume);
                    damage = Mathf.Clamp(damage, 0, maxDamage);
                    logger.LogDebug("Damage: " + damage); // temp

                    float drunkness = damage / (float)maxDamage * 0.3f;
                    logger.LogDebug("Drunkness: " + drunkness);
                    player.drunkness = drunkness;

                    if (ItemSFX.volume >= VolumeToStartDamagePlayers)
                    {
                        logger.LogDebug($"Damaging player with {damage} damage.");
                        int newHealth = player.health - damage;
                        player.DamagePlayerClientRpc(damage, newHealth);
                    }
                }
            }
        }
    }
}