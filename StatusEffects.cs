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
    public class RandomIntervalActionEffect(BoundedRange randomInterval, Action action, bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        BoundedRange randomInterval = randomInterval;
        Action action = action;

        float timeSinceLastInterval;
        float nextInterval;

        public override void OnApply()
        {
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override void OnReapply(StatusEffect effect)
        {
            throw new NotImplementedException();
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

    public class IntervalActionEffect(float interval, Action action, bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        float interval = interval;
        Action action = action;

        float timeSinceLastInterval;

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

    public class OnRemoveActionEffect(Action action, bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        Action action = action;

        public override void OnRemove()
        {
            action.Invoke();
        }
    }

    public class TickActionEffect(Action<float> action, bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        Action<float> action = action;

        public override void OnTick(float deltaTime)
        {
            action.Invoke(deltaTime);
        }
    }

    public class ChanceTickActionEffect(float chancePerSecond, Action action, bool removeOnDeath = true, float duration = 0) : StatusEffect(id, removeOnDeath, duration)
    {
        float chance = chancePerSecond;
        Action action = action;

        public override void OnTick(float deltaTime)
        {
            if (Utils.randomLocal.NextFloat(0f, 1f) < Mathf.Clamp01(chance) * deltaTime)
                action.Invoke();
        }
    }

    public class ConditionalActionEffect(Func<bool> condition, Action action, bool removeOnTrigger, float cooldown = 0f, int maxTriggerCount = 0, bool removeOnDeath = true, float duration = 0)
    : StatusEffect(id, removeOnDeath, duration)
    {
        Func<bool> condition = condition;
        Action action = action;
        bool removeOnTrigger = removeOnTrigger;
        float cooldown = cooldown;
        int maxTriggerCount = maxTriggerCount;

        float timeSinceLastTrigger;
        int triggerCount;

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

    public class RandomAudioEffect(string audioLibraryId, BoundedRange randomInterval, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0, string animationName = "", float animationTime = 0, bool removeOnDeath = true, float duration = 0f) : StatusEffect(removeOnDeath, duration)
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

        public override void OnReapply(StatusEffect effect)
        {
            LerpValueEffect? e = effect as LerpValueEffect;
            if (e == null) { return; }
            setter = e.setter;
            startValue = e.startValue;
            endValue = e.endValue;
            setter.Invoke(startValue);
        }
    }

    public class MaxSprintCapEffect(float sprintCap, bool removeOnDeath = true, float duration = 0) : StatusEffect(removeOnDeath, duration)
    {
        float sprintCap = sprintCap;

        public override void OnReapply(StatusEffect effect)
        {
            throw new NotImplementedException();
        }

        public override void OnTick(float deltaTime)
        {
            localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0, sprintCap);
        }
    }

    public class SprintSpeedCapEffect(float sprintSpeedCap, bool removeOnDeath = true, float duration = 0) : StatusEffect(removeOnDeath, duration)
    {
        float sprintSpeedCap = sprintSpeedCap;

        const float minRange = 1f;
        const float maxRange = 2.5f;

        public override void OnTick(float deltaTime)
        {
            float cap = Mathf.Clamp(sprintSpeedCap, minRange, maxRange);
            localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, sprintSpeedCap);
        }
    }
}