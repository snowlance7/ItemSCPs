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

        public static Dictionary<int, float> contributions = new Dictionary<int, float>();
        public static int previousContributionsID = 0;

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

            int id = previousContributionsID++;
            previousContributionsID = id;
            contributions[id] = 0f;

            StatusEffectController.Instance.ApplyEffect(new CurveValueEffect(value =>
            {
                contributions[id] = Mathf.Lerp(0f, 5f, value);
                RecalculateSprintTime();
            }, intensityOverTime, effectDuration, "scp207_1", $"scp207_1_{id}", onRemove: () =>
            {
                contributions.Remove(id);
                RecalculateSprintTime();
            }));
        }

        static void RecalculateSprintTime()
        {
            float total = 0f;
            foreach (var v in contributions.Values)
                total += v;
            localPlayer.sprintTime = total;
        }
    }
}