using GameNetcodeStuff;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ItemSCPs.Plugin;


//localPlayer.sprintMeter 0-1
//localPlayer.sprintTime 11, idk what this does
//localPlayer.sprintMultiplier 1-2.5, controls sprint speed

namespace ItemSCPs.SCP
{
    internal class SCP207Behavior : PhysicsProp, ISCP
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public AudioSource audioSource = null!;
        public GameObject capObject = null!;
        public AnimationCurve intensityOverTime = null!;
        public MeshRenderer liquidRenderer = null!;

        public static Dictionary<int, float> contributions = new();

        public static int previousContributionsID = 0;
        public static bool heartAttackLocalPlayer = false;

        PlayerControllerB previousPlayerHeldBy = null!;

        bool drinking;
        float drinkAmountLeft;
        float drinkingTime;

        Coroutine? drinkingRoutine;

        Vector3 drinkingPositionOffset = new Vector3(0f, 0.1f, 0.1f);
        Vector3 drinkingRotationOffset = new Vector3(50, 170, 0);

        const float effectDuration = 1200f;
        const float drinkTimePerBottle = 10f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.07f, 0.1f, -0.01f);
            itemProperties.rotationOffset = new Vector3(80, 90, 0);
            itemProperties.floorYOffset = 90;

            itemProperties.grabAnim = "HoldPatcherTool";

            itemProperties.syncUseFunction = true;

            itemProperties.toolTips = ["Drink [Hold LMB]"];
        }

        public override void Start()
        {
            base.Start();
            drinkAmountLeft = drinkTimePerBottle;
        }

        public override void Update()
        {
            base.Update();
            if (drinking)
            {
                drinkAmountLeft -= Time.deltaTime;
                drinkingTime += Time.deltaTime;

                if (drinkAmountLeft <= 0f)
                {
                    drinking = false;
                    audioSource.Stop();
                    liquidRenderer.enabled = false;

                    if (base.IsOwner)
                    {
                        if (drinkingTime > 0f && !TESTING.immunity)
                            ApplyEffect(drinkingTime);

                        previousPlayerHeldBy!.activatingItem = false;
                        previousPlayerHeldBy!.playerBodyAnimator.SetBool("useTZPItem", false);
                    }
                }
            }
        }

        public override void LateUpdate()
        {
            if (parentObject != null)
            {
                base.transform.rotation = parentObject.rotation;
                base.transform.Rotate(drinking ? drinkingRotationOffset : itemProperties.rotationOffset);
                base.transform.position = parentObject.position;
                Vector3 positionOffset = drinking ? drinkingPositionOffset : itemProperties.positionOffset;
                positionOffset = parentObject.rotation * positionOffset;
                base.transform.position += positionOffset;
            }
            if (rotateObject)
            {
                base.transform.Rotate(new Vector3(0f, Time.deltaTime * 60f, 0f), Space.World);
            }
            if (radarIcon != null)
            {
                radarIcon.position = base.transform.position;
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true) // Synced
        {
            base.ItemActivate(used, buttonDown);

            capObject.SetActive(false);

            if (buttonDown)
            {
                previousPlayerHeldBy = playerHeldBy;

                drinkingTime = 0f;
                if (drinkAmountLeft <= 0f)
                {
                    if (base.IsOwner)
                        previousPlayerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
                    return;
                }

                StopDrinkRoutine();
                drinkingRoutine = StartCoroutine(DrinkRoutine());
            }
            else
            {
                StopDrinkRoutine();
                drinking = false;
                audioSource.Stop();
                liquidRenderer.enabled = false;

                if (base.IsOwner && drinkingTime > 0f && !TESTING.immunity)
                {
                    ApplyEffect(drinkingTime);
                }
            }

            if (base.IsOwner)
            {
                previousPlayerHeldBy.activatingItem = buttonDown;
                previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
            }
        }

        void StopDrinkRoutine()
        {
            if (drinkingRoutine != null)
            {
                StopCoroutine(drinkingRoutine);
                drinkingRoutine = null;
            }
        }

        IEnumerator DrinkRoutine()
        {
            yield return new WaitForSeconds(0.7f);

            drinking = true;
            if (base.IsOwner)
                audioSource.Play();
            drinkingRoutine = null;
        }

        void ApplyEffect(float amount)
        {
            int id = previousContributionsID++;
            previousContributionsID = id;
            contributions[id] = 0f;

            localPlayer.StatusEffectController().ApplyEffect(new CurveValueEffect(value =>
            {
                contributions[id] = Mathf.Lerp(0f, amount, value);
                float total = GetTotalContributions();
                localPlayer.sprintTime = Mathf.Max(11 + total, localPlayer.sprintTime);
                if (total > 10 && !heartAttackLocalPlayer)
                {
                    heartAttackLocalPlayer = true;
                    Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], NetworkHandler.Instance.heartbeatFastSFX, audibleNoiseID: -1);
                    localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        if (!localPlayer.isPlayerDead && localPlayer.isPlayerControlled)
                            localPlayer.KillPlayer(Vector3.zero);
                        heartAttackLocalPlayer = false;
                    }, "scp207", "heart attack", 6));
                }
            }, intensityOverTime, effectDuration, "scp207", $"scp207_{id}", onRemove: (effect) =>
            {
                contributions.Remove(id);
                localPlayer.sprintTime = Mathf.Max(11 + GetTotalContributions(), localPlayer.sprintTime);
            }));

            localPlayer.StatusEffectController().ApplyEffect(new ConditionalActionEffect(() => GetTotalContributions() > 7.5f, () => Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], NetworkHandler.Instance.heartbeatSlowSFX, audibleNoiseID: -1), false, "scp207", 30, 0, "scp207_heartbeatSlow", effectDuration));
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