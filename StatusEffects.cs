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
    public class RandomIntervalActionEffect(BoundedRange randomInterval, Action action, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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

    public class IntervalActionEffect(float interval, Action action, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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

    public class OnRemoveActionEffect(Action action, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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

    public class TickActionEffect(Action<float> action, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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

    public class ChanceTickActionEffect(float chancePerSecond, Action action, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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

    public class ConditionalActionEffect(Func<bool> condition, Action action, bool removeOnTrigger, float cooldown = 0f, int maxTriggerCount = 0, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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

    public class LerpValueEffect(Action<float> setter, float startValue, float endValue, float duration, string id = "", bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
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
    public class RandomIntervalPhaseActionEffect(BoundedRange randomInterval, BoundedRange randomPhaseDuration, Action action, string id = "", float duration = 0, bool removeOnDeath = true, bool pauseInOrbit = true) : StatusEffect(id, duration, removeOnDeath, pauseInOrbit)
    {
        BoundedRange randomInterval = randomInterval;
        BoundedRange randomPhaseDuration = randomPhaseDuration;
        Action action = action;

        float timeSinceLastInterval;
        float nextInterval;

        float phaseTimer;

        public override void OnApply()
        {
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override bool OnReapply(StatusEffect effect)
        {
            var e = (RandomIntervalPhaseActionEffect)effect;
            randomInterval = e.randomInterval;
            randomPhaseDuration = e.randomPhaseDuration;
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
            if (phaseTimer <= 0)
                timeSinceLastInterval += deltaTime;

            if (timeSinceLastInterval > nextInterval)
            {
                timeSinceLastInterval = 0f;
                nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
                phaseTimer = randomPhaseDuration.GetRandomInRange(Utils.randomLocal);
            }

            if (phaseTimer > 0)
            {
                phaseTimer -= deltaTime;
                action.Invoke();
            }
        }
    }
}