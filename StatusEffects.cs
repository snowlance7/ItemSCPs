using Dawn.Utils;
using ItemSCPs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemSCPs.Plugin;

//localPlayer.sprintMeter 0-1
//localPlayer.sprintTime 11, idk what this does
//localPlayer.sprintMultiplier 1-2.5, controls sprint speed

/*
ShortFallLanding (Trigger) - coughing small motion
SpawnPlayer (Trigger) - puking
startCrouching (Trigger) - force crouch, specialanimation time for duration
Damage (Trigger) - hands in air
Overheat (Trigger) - hands in air lower
SA_Typing (Trigger) - puking motion, head forward?
SA_stopAnimation (Trigger)
SA_ChargeItem (Trigger) - hand out
SA_PushLeverBack (Trigger) - forces screen to middle and does quick animation
*/

namespace ItemSCPs
{
    public class RandomIntervalActionEffect(BoundedRange randomInterval, Action action, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        BoundedRange randomInterval = randomInterval;
        Action action = action;

        float timeSinceLastInterval;
        float nextInterval;

        public override void OnApply()
        {
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (RandomIntervalActionEffect)effect;
            randomInterval = e.randomInterval;
            action = e.action;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            timeSinceLastInterval = 0f;
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
            elapsedTime = 0f;
            return true;
        }

        public override void OnTick(float deltaTime)
        {
            timeSinceLastInterval += deltaTime;

            if (timeSinceLastInterval > nextInterval)
            {
                timeSinceLastInterval = 0f;
                nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);

                action.Invoke();
            }
        }
    }

    public class IntervalActionEffect(float interval, Action action, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        float interval = interval;
        Action action = action;

        float timeSinceLastInterval;

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (IntervalActionEffect)effect;
            interval = e.interval;
            action = e.action;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            timeSinceLastInterval = 0f;
            elapsedTime = 0f;
            return true;
        }

        public override void OnTick(float deltaTime)
        {
            timeSinceLastInterval += deltaTime;

            if (timeSinceLastInterval > interval)
            {
                timeSinceLastInterval = 0f;
                action.Invoke();
            }
        }
    }

    public class OnRemoveActionEffect(Action action, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        Action action = action;

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (OnRemoveActionEffect)effect;
            action = e.action;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            elapsedTime = 0f;
            return true;
        }

        public override void OnRemove()
        {
            action.Invoke();
        }
    }

    public class TickActionEffect(Action<float> action, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        Action<float> action = action;

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (TickActionEffect)effect;
            action = e.action;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            elapsedTime = 0f;
            return true;
        }

        public override void OnTick(float deltaTime)
        {
            action.Invoke(deltaTime);
        }
    }

    public class ChanceTickActionEffect(float chancePerSecond, Action action, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        float chance = chancePerSecond;
        Action action = action;

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (ChanceTickActionEffect)effect;
            chance = e.chance;
            action = e.action;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            elapsedTime = 0f;
            return true;
        }

        public override void OnTick(float deltaTime)
        {
            if (Utils.randomLocal.NextFloat(0f, 1f) < Mathf.Clamp01(chance) * deltaTime)
                action.Invoke();
        }
    }

    public class ConditionalActionEffect(Func<bool> condition, Action action, bool removeOnTrigger, float cooldown = 0f, int maxTriggerCount = 0, string id = "", bool removeOnDeath = true, float duration = 0)
    : StatusEffect(id, removeOnDeath, duration)
    {
        Func<bool> condition = condition;
        Action action = action;
        bool removeOnTrigger = removeOnTrigger;
        float cooldown = cooldown;
        int maxTriggerCount = maxTriggerCount;

        float timeSinceLastTrigger;
        int triggerCount;

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (ConditionalActionEffect)effect;

            condition = e.condition;
            action = e.action;
            removeOnTrigger = e.removeOnTrigger;
            cooldown = e.cooldown;
            maxTriggerCount = e.maxTriggerCount;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;

            timeSinceLastTrigger = 0f;
            triggerCount = 0;
            elapsedTime = 0f;
            return true;
        }

        public override void OnTick(float deltaTime)
        {
            timeSinceLastTrigger += deltaTime;

            if (condition() && timeSinceLastTrigger > cooldown)
            {
                timeSinceLastTrigger = 0f;
                triggerCount++;
                action.Invoke();

                if (removeOnTrigger || (maxTriggerCount > 0 && triggerCount >= maxTriggerCount))
                    Remove();
            }
        }
    }

    public class RandomIntervalAudioEffect(string audioLibraryId, BoundedRange randomInterval, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0, string animationName = "", float animationTime = 0, string id = "", bool removeOnDeath = true, float duration = 0f) : StatusEffect(id, removeOnDeath, duration)
    {
        string audioLibraryId = audioLibraryId;
        BoundedRange randomInterval = randomInterval;
        int bodyPartIndex = bodyPartIndex;
        float volume = volume;
        float min3DDistance = min3DDistance;
        float max3DDistance = max3DDistance;
        float cutoffFrequency = cutoffFrequency;
        int audibleNoiseID = audibleNoiseID;
        string animationName = animationName;
        float animationTime = animationTime;

        float timeSinceLastPlayback;
        float nextPlaybackTime;

        public override void OnApply()
        {
            nextPlaybackTime = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (RandomIntervalAudioEffect)effect;

            audioLibraryId = e.audioLibraryId;
            randomInterval = e.randomInterval;
            bodyPartIndex = e.bodyPartIndex;
            volume = e.volume;
            min3DDistance = e.min3DDistance;
            max3DDistance = e.max3DDistance;
            cutoffFrequency = e.cutoffFrequency;
            audibleNoiseID = e.audibleNoiseID;
            animationName = e.animationName;
            animationTime = e.animationTime;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            elapsedTime = 0f;

            timeSinceLastPlayback = 0f;
            nextPlaybackTime = randomInterval.GetRandomInRange(Utils.randomLocal);
            return true;
        }

        public override void OnTick(float deltaTime)
        {
            timeSinceLastPlayback += deltaTime;

            if (timeSinceLastPlayback > nextPlaybackTime)
            {
                timeSinceLastPlayback = 0f;
                nextPlaybackTime = randomInterval.GetRandomInRange(Utils.randomLocal);

                if (animationName != "" && animationTime > 0)
                {
                    localPlayer.PlayQuickSpecialAnimation(animationTime);
                    localPlayer.playerBodyAnimator.SetTrigger(animationName);
                }

                controller.PlayRandomClipServerRpc(audioLibraryId, bodyPartIndex, volume, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
            }
        }
    }

    public class LerpValueEffect(Action<float> setter, float startValue, float endValue, string id = "", bool removeOnDeath = true, float duration = 1f) : StatusEffect(id, removeOnDeath, duration)
    {
        Action<float> setter = setter;

        float startValue = startValue;
        float endValue = endValue;

        public override void OnApply()
        {
            setter.Invoke(startValue);
        }

        public override void OnTick(float deltaTime)
        {
            elapsedTime += deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            float value = Mathf.Lerp(startValue, endValue, t);

            setter.Invoke(value);
        }

        public override void OnRemove()
        {
            setter.Invoke(endValue);
        }

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (LerpValueEffect)effect;
            setter = e.setter;
            startValue = e.startValue;
            endValue = e.endValue;
            removeOnDeath = e.removeOnDeath;
            duration = e.duration;
            setter.Invoke(startValue);
            elapsedTime = 0f;
            return true;
        }
    }

    public class MaxSprintCapEffect(float sprintCap, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        float sprintCap = sprintCap;

        public override void OnApply()
        {
            sprintCap = Mathf.Clamp01(sprintCap);
        }

        public override void OnTick(float deltaTime)
        {
            localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0, sprintCap);
        }

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (MaxSprintCapEffect)effect;
            sprintCap = Mathf.Min(sprintCap, e.sprintCap);
            sprintCap = Mathf.Clamp01(sprintCap);
            removeOnDeath = e.removeOnDeath;
            duration = Mathf.Max(duration, e.duration);
            elapsedTime = 0f;
            return true;
        }
    }

    public class SprintSpeedCapEffect(float sprintSpeedCap, string id = "", bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        public override bool AllowMultipleInstances => false;
        float sprintSpeedCap = sprintSpeedCap;

        const float minRange = 1f;
        const float maxRange = 2.5f;

        public override void OnApply()
        {
            sprintSpeedCap = Mathf.Clamp(sprintSpeedCap, minRange, maxRange);
        }

        public override void OnTick(float deltaTime)
        {
            localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, sprintSpeedCap);
        }

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (SprintSpeedCapEffect)effect;
            sprintSpeedCap = Mathf.Min(sprintSpeedCap, e.sprintSpeedCap);
            sprintSpeedCap = Mathf.Clamp(sprintSpeedCap, minRange, maxRange);
            removeOnDeath = e.removeOnDeath;
            duration = Mathf.Max(duration, e.duration);
            elapsedTime = 0f;
            return true;
        }
    }
}