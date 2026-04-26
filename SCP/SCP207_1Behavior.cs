using SnowyLib;
using System.Collections.Generic;
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
        public static bool heartAttackLocalPlayer = false;

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
            float contribution = UnityEngine.Random.Range(3.5f, 6f);

            StatusEffectController.Instance.ApplyEffect(new CurveValueEffect(value =>
            {
                contributions[id] = Mathf.Lerp(0f, contribution, value);
                float total = GetTotalContributions();
                localPlayer.sprintTime = total;
                if (total > 10 && !heartAttackLocalPlayer)
                {
                    heartAttackLocalPlayer = true;
                    Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], ItemSCPsNetworkHandler.Instance.heartbeatFastSFX, audibleNoiseID: -1);
                    StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        if (!localPlayer.isPlayerDead)
                            localPlayer.KillPlayer(Vector3.zero);
                        heartAttackLocalPlayer = false;
                    }, "scp207_1", "heart attack", 6));
                }
            }, intensityOverTime, effectDuration, "scp207_1", $"scp207_1_{id}", onRemove: () =>
            {
                contributions.Remove(id);
                localPlayer.sprintTime = GetTotalContributions();
            }));

            StatusEffectController.Instance.ApplyEffect(new ConditionalActionEffect(() => GetTotalContributions() > 7.5f, () => Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], ItemSCPsNetworkHandler.Instance.heartbeatSlowSFX, audibleNoiseID: -1), false, "scp207_1", 30, 0, "scp207_1_heartbeatSlow", effectDuration, true, true));
        }

        static float GetTotalContributions()
        {
            float total = 0f;
            foreach (var v in contributions.Values)
                total += v;
            return total;
        }
    }
}