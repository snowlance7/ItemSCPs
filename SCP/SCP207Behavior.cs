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

// UPDATE: Crate of 207's similar to the bottles scrap but 207 instead and you can take them out of the crate
// UPDATE: A 207 vending machine outside the company building that has a chance of appearing

namespace ItemSCPs.SCP
{
    internal class SCP207Behavior : PhysicsProp, ISCP // UPDATE: Make it so liquid slowly depletes visually
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

        static float totalContributions;

        Vector3 drinkingPositionOffset = new Vector3(0f, 0.1f, 0.1f);
        Vector3 drinkingRotationOffset = new Vector3(50, 170, 0);

        Vector3 currentPositionOffset;
        Vector3 currentRotationOffset;

        const float effectDuration = 1200f;
        const float drinkTimePerBottle = 10f;
        const float maxSprintIncreaseBeforeHeartAttack = 9f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.07f, 0.1f, -0.01f);
            itemProperties.rotationOffset = new Vector3(80, 90, 0);
            itemProperties.floorYOffset = 90;

            currentPositionOffset = itemProperties.positionOffset;
            currentRotationOffset = itemProperties.rotationOffset;

            itemProperties.grabAnim = "HoldPatcherTool";

            itemProperties.syncUseFunction = true;

            itemProperties.toolTips = ["Drink [Hold LMB]"];
        }

        public override void Start()
        {
            base.Start();
            drinkAmountLeft = drinkTimePerBottle;
        }

        [StaticUpdate]
        public static void StaticUpdate()
        {
            totalContributions = GetTotalContributions();
            if (totalContributions <= 0) { return; }

            float contributionsNormalized = Mathf.Clamp01(totalContributions / maxSprintIncreaseBeforeHeartAttack);

            localPlayer.sprintTime = Mathf.Max(11 + totalContributions, localPlayer.sprintTime);

            localPlayer.sprintMeter = Mathf.Clamp01(localPlayer.sprintMeter + Time.deltaTime / (localPlayer.sprintTime + Mathf.Lerp(15f, 5f, contributionsNormalized)));

            localPlayer.sprintMultiplier = Mathf.Max(localPlayer.sprintMultiplier, Mathf.Lerp(1f, 3f, contributionsNormalized));

            if (totalContributions > maxSprintIncreaseBeforeHeartAttack && !heartAttackLocalPlayer)
            {
                logger.LogDebug($"Total contributions {totalContributions} > {maxSprintIncreaseBeforeHeartAttack}, heart attack time");
                heartAttackLocalPlayer = true;
                Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], NetworkHandler.Instance.heartbeatFastSFX, audibleNoiseID: -1);
                localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect((effect) =>
                {
                    heartAttackLocalPlayer = false;
                    if (!effect.timeExpired) { return; }
                    if (!localPlayer.isPlayerDead && localPlayer.isPlayerControlled)
                        localPlayer.KillPlayer(Vector3.zero);
                }, "scp207", "heart attack", 6));
            }
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
                    StopDrinkRoutine();

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
                base.transform.Rotate(currentRotationOffset);
                base.transform.position = parentObject.position;
                Vector3 positionOffset = currentPositionOffset;
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

        public override void DiscardItem()
        {
            base.DiscardItem();
            previousPlayerHeldBy.activatingItem = false;
            previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", false);
            StopDrinkRoutine();
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

            currentRotationOffset = itemProperties.rotationOffset;
            currentPositionOffset = itemProperties.positionOffset;

            drinking = false;
            audioSource.Stop();
            liquidRenderer.enabled = drinkAmountLeft > 0f;
        }

        IEnumerator DrinkRoutine()
        {
            float elapsedTime = 0f;

            while (elapsedTime < 1f)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.Clamp01(elapsedTime / 0.5f);

                currentPositionOffset = Vector3.Lerp(itemProperties.positionOffset, drinkingPositionOffset, t);
                currentRotationOffset = Vector3.Lerp(itemProperties.rotationOffset, drinkingRotationOffset, t);
                yield return null;
            }

            drinking = true;
            if (base.IsOwner)
                audioSource.Play();
            drinkingRoutine = null;
        }

        void ApplyEffect(float amount)
        {
            int id = previousContributionsID;
            previousContributionsID++;
            contributions[id] = 0f;

            localPlayer.StatusEffectController().ApplyEffect(new CurveValueEffect(value =>
            {
                contributions[id] = Mathf.Lerp(0f, amount, value);
            }, intensityOverTime, effectDuration, "scp207", $"scp207_{id}", onRemove: (effect) =>
            {
                contributions.Remove(id);
            }));

            localPlayer.StatusEffectController().ApplyEffect(new ConditionalActionEffect(() => GetTotalContributions() > maxSprintIncreaseBeforeHeartAttack * 0.5f, () => Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], NetworkHandler.Instance.heartbeatSlowSFX, audibleNoiseID: -1), false, "scp207", 30, 0, "scp207_heartbeatSlow", effectDuration));
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