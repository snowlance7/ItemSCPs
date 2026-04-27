using GameNetcodeStuff;
using SnowyLib;
using System.Collections.Generic;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP207_1Behavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public GameObject capObject;
        public AnimationCurve intensityOverTime;
        public Animator animator;
#pragma warning restore CS8618

        public static Dictionary<int, float> contributions = new();

        public static int previousContributionsID = 0;
        public static bool heartAttackLocalPlayer = false;

        PlayerControllerB? previousPlayerHeldBy;

        bool drinking;
        float drinkAmountLeft;
        float drinkAmountNormalized => drinkAmountLeft / drinkTimePerBottle;
        float drinkingTime;

        int hashDrinkTime;

        // Configs
        float effectDuration = 1200f;
        float drinkTimePerBottle = 10f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;

            itemProperties.syncUseFunction = true;

            itemProperties.toolTips = ["Drink [Hold LMB]"];
        }

        public override void Start()
        {
            base.Start();
            drinkAmountLeft = drinkTimePerBottle;
            hashDrinkTime = Animator.StringToHash("drinkTime");
        }

        public override void Update()
        {
            base.Update();
            if (drinking)
            {
                drinkAmountLeft -= Time.deltaTime;
                drinkingTime += Time.deltaTime;
                animator.SetFloat(hashDrinkTime, drinkAmountNormalized);

                if (drinkAmountLeft <= 0f)
                {
                    drinking = false;
                    audioSource.Stop();

                    if (base.IsOwner)
                    {
                        if (drinkingTime > 0f)
                            ApplyEffect(drinkingTime);

                        previousPlayerHeldBy!.activatingItem = false;
                        previousPlayerHeldBy!.playerBodyAnimator.SetBool("useTZPItem", false);
                    }
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true) // Synced
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown)
            {
                previousPlayerHeldBy = playerHeldBy;

                drinkingTime = 0f;
                if (drinkAmountLeft <= 0f)
                {
                    previousPlayerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
                    return;
                }

                drinking = true;
                if (base.IsOwner)
                    audioSource.Play();
            }
            else
            {
                drinking = false;
                audioSource.Stop();

                if (base.IsOwner && drinkingTime > 0f)
                {
                    ApplyEffect(drinkingTime);
                }
            }

            if (base.IsOwner)
            {
                previousPlayerHeldBy!.activatingItem = buttonDown;
                previousPlayerHeldBy!.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
            }
        }

        void ApplyEffect(float amount)
        {
            int id = previousContributionsID++;
            previousContributionsID = id;
            contributions[id] = 0f;

            StatusEffectController.Instance.ApplyEffect(new CurveValueEffect(value =>
            {
                contributions[id] = Mathf.Lerp(0f, amount, value);
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