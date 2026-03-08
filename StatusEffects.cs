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

    public class RandomInterruptEffect(BoundedRange randomInterval, AudioClip[] audioClips, string animationName = "", float duration = 0f) : StatusEffect(duration)
    {
        BoundedRange randomInterval = randomInterval;
        AudioClip[] audioClips = audioClips;
        string animationName = animationName;

        float timeSinceLastInterrupt;
        float nextInterruptTime;

        public override void OnApply()
        {
            nextInterruptTime = randomInterval.GetRandomInRange(Utils.randomLocal);
        }

        public override void OnTick(float deltaTime)
        {
            timeSinceLastInterrupt += deltaTime;

            if (timeSinceLastInterrupt > nextInterruptTime)
            {
                timeSinceLastInterrupt = 0f;
                nextInterruptTime = randomInterval.GetRandomInRange(Utils.randomLocal);

                //controller.networkAudioSource.PlayOneShot(audioClips[Utils.randomLocal.Next(0, audioClips.Length)]); // TODO: Test this
            }
        }
    }
}