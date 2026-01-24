using BepInEx;
using BepInEx.Configuration;
using Dawn;
using System;
using System.Collections.Generic;
using System.Text;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(DawnLib.PLUGIN_GUID)]
    public class ConfigManager : Plugin
    {
        #region Snowys Items

        // SCP-498 Configs
        public static ConfigEntry<float> configTimeBeforeAlarmStart;
        public static ConfigEntry<float> configTimeBetweenAlarms;
        public static ConfigEntry<float> configTimeBeforeVolumeIncrease;
        public static ConfigEntry<float> configVolumeIncreaseAmount;
        public static ConfigEntry<float> configStartingAlarmVolume;
        public static ConfigEntry<float> configVolumeToStartDamagePlayers;
        public static ConfigEntry<int> configAlarmType;
        public static ConfigEntry<float> configMaxVolumeMinDistance;
        public static ConfigEntry<int> config498MaxDamage;

        // SCP-735 Configs
        public static ConfigEntry<float> config735RandomPhrasesInterval;
        public static ConfigEntry<float> config735RandomPhrasesChance;
        public static ConfigEntry<float> config735PhraseCooldown;

        // SCP-012 Configs
        public static ConfigEntry<float> config012ActivationRange;
        public static ConfigEntry<float> config012ForceLookIntensity;
        public static ConfigEntry<float> config012ForceWalkIntensity;
        public static ConfigEntry<float> config012DeathZone;
        public static ConfigEntry<int> config012DamagePerSecond;

        // SCP-983 Configs
        public static ConfigEntry<int> config9831MinValue;
        public static ConfigEntry<int> config9831MaxValue;
        public static ConfigEntry<int> config9832MinValue;
        public static ConfigEntry<int> config9832MaxValue;

        // SCP-1079 Configs
        public static ConfigEntry<int> config1079MinAmountInBox;
        public static ConfigEntry<int> config1079MaxAmountInBox;
        public static ConfigEntry<int> config10791Damage;

        // SCP-2536 Configs
        public static ConfigEntry<float> config2536PlayerDistanceToSpawnGift;
        public static ConfigEntry<float> config2536DespawnTime;
        public static ConfigEntry<bool> config2536UseCopyRightFreeMusic;

        #endregion

        #region Rats Items
        //SCP-201
        public static ConfigEntry<float> config201ActivationRange;
        public static ConfigEntry<int> config201DamagePerSecond;

        //SCP 207
        public static ConfigEntry<bool> config207SCPSLstyle;
        public static ConfigEntry<float> config207Speed;

        //SCP-018 Configs
        public static ConfigEntry<bool> config018slStyle;
        public static ConfigEntry<float> config018maxSpeed;

        #endregion

        public static void LoadConfigs()
        {
            #region Snowys Items

            // SCP-498
            configTimeBeforeAlarmStart = Instance.Config.Bind("SCP-498", "Time Before Alarm Start", 120f, "Time in seconds before the alarm starts. This gives everyone a grace period to get inside before it starts going off.");
            configTimeBetweenAlarms = Instance.Config.Bind("SCP-498", "Time Between Alarms", 150f, "Time in seconds between alarms.");
            configTimeBeforeVolumeIncrease = Instance.Config.Bind("SCP-498", "Time Before Volume Increases", 10f, "How much effectTime should pass before the volume of the alarm increases.");
            configVolumeIncreaseAmount = Instance.Config.Bind("SCP-498", "Volume Increase Amount", 0.025f, "How much volume should be added to the alarm each effectTime it increases.");
            configStartingAlarmVolume = Instance.Config.Bind("SCP-498", "Starting Alarm Volume", 0f, "Starting volume of the alarm when it goes off.");
            configVolumeToStartDamagePlayers = Instance.Config.Bind("SCP-498", "Volume To Start Damaging Players", 0.75f, "Minimum volume needed to start damaging players.");
            configAlarmType = Instance.Config.Bind("SCP-498", "Alarm Type", 0, "0 = Extra Loud (default), 1 = Four Beeps, 2 = Alien, 3 = Annoying");
            configMaxVolumeMinDistance = Instance.Config.Bind("SCP-498", "Min Distance To Damage Players", 10f, "Minimum distance to play alarm at max volume, any distance farther than this and the volume will start to lower.");
            config498MaxDamage = Instance.Config.Bind("SCP-498", "Max Damage", 15, "Maximum damage the alarm can do to a player.");

            // SCP-735
            config735RandomPhrasesInterval = Instance.Config.Bind("SCP-735", "Random Phrases Interval", 10f, "Time in seconds between random phrases. Set to 0 to disable.");
            config735RandomPhrasesChance = Instance.Config.Bind("SCP-735", "Random Phrases Chance", 50f, "Chance of a random phrase being used. Set to 100 to always say a random phrase.");
            config735PhraseCooldown = Instance.Config.Bind("SCP-735", "Phrase Cooldown", 7f, "Time in seconds between phrases. Set to 0 to ignore cooldown.");

            // SCP-012
            config012ActivationRange = Instance.Config.Bind("SCP-012", "Activation Range", 10f, "Activation range for SCP-012. Players will start being forced to look and walk to 012 within this range.");
            config012ForceLookIntensity = Instance.Config.Bind("SCP-012", "Force Look Intensity", 0.25f, "Force look intensity for SCP-012. How fast a player will be forced to look at 012 once in activation range.");
            config012ForceWalkIntensity = Instance.Config.Bind("SCP-012", "Force Walk Intensity", 1f, "Force walk intensity for SCP-012. How fast a player will be forced to walk towards 012 once in activation range.");
            config012DeathZone = Instance.Config.Bind("SCP-012", "Death Zone", 5f, "The distance to 012 in which the player can no longer resist the effects of 012 and will essentially be already dead unless teleported or intervened in some way.");
            config012DamagePerSecond = Instance.Config.Bind("SCP-012", "Damage Per Second", 4, "Damage per second players will take while they are 'using' 012.");

            // SCP-983
            config9831MinValue = Instance.Config.Bind("SCP-983-1", "Min Value", 10, "Minimum scrap value for SCP-983-1.");
            config9831MaxValue = Instance.Config.Bind("SCP-983-1", "Max Value", 100, "Maximum scrap value for SCP-983-1.");
            config9832MinValue = Instance.Config.Bind("SCP-983-2", "Min Value", 150, "Minimum scrap value for SCP-983-2.");
            config9832MaxValue = Instance.Config.Bind("SCP-983-2", "Max Value", 500, "Maximum scrap value for SCP-983-2.");

            // SCP-1079
            config1079MinAmountInBox = Instance.Config.Bind("SCP-1079", "Min Amount In Box", 1, "Minimum amount of Bon Bons in a box.");
            config1079MaxAmountInBox = Instance.Config.Bind("SCP-1079", "Max Amount In Box", 5, "Maximum amount of Bon Bons in a box.");
            config10791Damage = Instance.Config.Bind("SCP-1079", "Damage", 45, "How much damage a single piece of Bon Bons candy does to the player.");

            // SCP-2536
            config2536UseCopyRightFreeMusic = Instance.Config.Bind("SCP-2536", "Use CopyRight Free Music", true, "Replaces the original music with a cover version.");
            config2536DespawnTime = Instance.Config.Bind("SCP-2536", "Despawn Time", 30f, "Time until SCP-2536-1 despawns when no players are around it.");
            config2536PlayerDistanceToSpawnGift = Instance.Config.Bind("SCP-2536", "Player Distance To Spawn Gift", 15f, "Distance to the tree when it spawns a player needs to be for it to spawn a gift for them.");

            // SCP-201
            config201ActivationRange = Instance.Config.Bind("SCP-201", "Activation Range", 5f, "Activation range for SCP-201.");
            config201DamagePerSecond = Instance.Config.Bind("SCP-201", "Damage Per Second", 100, "Damage per second players will take while they are 'using' 201.");

            #endregion

            #region Rats Items

            //SCP-207
            config207SCPSLstyle = Instance.Config.Bind("SCP-207", "SCP-207 SL", false, "Makes it work like SL");
            config207Speed = Instance.Config.Bind("SCP-207", "SCP-207 speed", 10.0f, "How fast SCP-207 makes you");

            //SCP-018
            config018slStyle = Instance.Config.Bind("SCP-018", "SL Style", false, "If it explodes or if it continues until you leave");
            config018maxSpeed = Instance.Config.Bind("SCP-018", "Max speed", 50f, "You can adjust value but do not go above 100f");
            #endregion
        }
    }
}
