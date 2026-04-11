using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP207_1Behavior : PhysicsProp
    {
#pragma warning disable CS8618
        public GameObject liquidObject;
        public GameObject capObject;
        public AudioClip drinkSFX;
        public AnimationCurve intensityOverTime;
#pragma warning restore CS8618

        public static Dictionary<float, float> contributions = new Dictionary<float, float>();

        float effectDuration = 1200f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;

            itemProperties.toolTips = ["Drink [LMB]"];
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            float seed = UnityEngine.Random.Range(0f, 1f);

            if (contributions.Count == 0)
            {
                contributions.Add(seed, 0f);
                StatusEffectController.Instance.ApplyEffect(new ConditionalActionEffect(() =>
                {
                    if (contributions.Count == 0)
                    {
                        return true;
                    }

                    float contribution = 0f;
                    foreach (var x in contributions)
                    {
                        contribution += x.Value;
                    }
                    localPlayer.sprintTime = contribution;
                    return false;
                }, () =>
                {
                    return;
                }, removeOnTrigger: true, "scp207_1"));
            }
            else
            {
                contributions.Add(seed, 0f);
            }

            StatusEffectController.Instance.ApplyEffect(new CurveValueEffect((value) => contributions[seed] = Mathf.Lerp(0f, 5f, value), intensityOverTime, effectDuration, "scp207_1"));
        }
    }
}