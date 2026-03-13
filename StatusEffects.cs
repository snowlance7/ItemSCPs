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

namespace ItemSCPs
{
    public class BleedingEffect(int amount, float interval, int damage) : StatusEffect
    {
        private readonly int amount = amount;
        private readonly float interval = interval;
        private readonly int damage = damage;

        public override void OnApply()
        {
            /*IEnumerator BleedRoutine(int amount, float interval, int damage)
            {
                yield return null;

                for (int i = 0; i < amount; i++)
                {
                    yield return new WaitForSeconds(interval);
                    localPlayer.DropBlood();
                    if (damage > 0)
                        localPlayer.DamagePlayer(damage);
                }

                effectRoutine = null;
                controller.RemoveEffect(this);
            }

            effectRoutine = controller.StartCoroutine(BleedRoutine(amount, interval, damage));*/
        }
    }

    public class MaxSprintCapEffect(float sprintCap, float duration) : StatusEffect(duration)
    {
        float sprintCap = sprintCap;

        public override void OnTick(float deltaTime)
        {
            localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0, sprintCap);
        }
    }

    public class SprintSpeedCapEffect(float sprintSpeedCap, float duration) : StatusEffect(duration)
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

    public class RandomAudioEffect(string audioLibraryId, BoundedRange randomInterval, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, bool interruptActions = false, string animationName = "", float duration = 0f) : StatusEffect(duration)
    {
        string audioLibraryId = audioLibraryId;
        BoundedRange randomInterval = randomInterval;
        int bodyPartIndex = bodyPartIndex;
        float volume = volume;
        float min3DDistance = min3DDistance;
        float max3DDistance = max3DDistance;
        float cutoffFrequency = cutoffFrequency;
        bool interruptActions = interruptActions;
        string animationName = animationName;

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

                if (animationName != "")
                    localPlayer.playerBodyAnimator.SetTrigger(animationName);

                if (interruptActions && animationName == "")
                    localPlayer.playerBodyAnimator.SetTrigger("Damage");

                controller.PlayRandomClipServerRpc(audioLibraryId, bodyPartIndex, volume, cutoffFrequency);
            }
        }
    }

    public class CustomRandomIntervalActionEffect(BoundedRange randomInterval, Action action, float duration = 0) : StatusEffect(duration)
    {
        BoundedRange randomInterval = randomInterval;
        Action action = action;

        float timeSinceLastInterval;
        float nextInterval;

        public override void OnApply()
        {
            nextInterval = randomInterval.GetRandomInRange(Utils.randomLocal);
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
}