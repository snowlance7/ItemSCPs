using GameNetcodeStuff;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using System.Collections;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    public class SCP420JBehavior : PhysicsProp, ISCP // TODO: Floating??? // TODO: Cant use charger to light???
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public AudioSource audioSource = null!;
        public ParticleSystem particleSystem = null!;
        public SkinnedMeshRenderer renderer = null!;
        public AudioClip exhaleSFX = null!;

        PlayerControllerB previousPlayerHeldBy = null!;

        bool hasFuel => fuel > 0;
        float fuel = 1f;
        float fuelUseMultiplier => inhaling ? 1.5f : 1f;

        bool hasBeenLit;

        bool inhaling => isUsing && hasBeenLit && hasFuel;
        float timeInhaling;

        bool isBurning => hasBeenLit && hasFuel;

        bool isUsing;

        Vector3 usingPositionOffset = new Vector3(0.05f, 0f, 0.2f);
        Vector3 usingRotationOffset = new Vector3(40f, 10f, 0f);

        Vector3 particleSystemStart = new Vector3(0f, 0.035f, 0.2f);
        Vector3 particleSystemEnd = new Vector3(0f, 0.0085f, -0.1326f);

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.03f, 0.3f, 0.12f);
            itemProperties.rotationOffset = new Vector3(-60f, 0f, 0f);
            itemProperties.floorYOffset = 90;
            itemProperties.syncUseFunction = true;
            itemProperties.syncDiscardFunction = true;
            itemProperties.itemIsTrigger = true;
            itemProperties.requiresBattery = true;
            itemProperties.batteryUsage = 0;
            itemProperties.holdButtonUse = true;
        }

        public override void Update()
        {
            if (hasBeenLit)
            {
                if (!hasFuel)
                {
                    particleSystem.Stop();
                    audioSource.Stop();
                    return;
                }
                if (inhaling && previousPlayerHeldBy == localPlayer)
                {
                    timeInhaling += Time.deltaTime;
                    previousPlayerHeldBy.drunknessInertia = Mathf.Clamp(previousPlayerHeldBy.drunknessInertia + Time.deltaTime / 1.75f * previousPlayerHeldBy.drunknessSpeed, 0.1f, 3f);
                    previousPlayerHeldBy.increasingDrunknessThisFrame = true;
                    previousPlayerHeldBy.sprintMeter = Mathf.Clamp(previousPlayerHeldBy.sprintMeter + Time.deltaTime / (previousPlayerHeldBy.sprintTime + 9f), 0f, 1f);
                }

                audioSource.volume = inhaling ? 1f : 0.5f;
                fuel -= Time.deltaTime / (38f * fuelUseMultiplier);
                renderer.SetBlendShapeWeight(0, Mathf.Lerp(100f, 0f, fuel));
                particleSystem.transform.localPosition = Vector3.Lerp(particleSystemEnd, particleSystemStart, fuel);
            }
            base.Update();
        }

        public override void LateUpdate()
        {
            if (parentObject != null)
            {
                base.transform.rotation = parentObject.rotation;
                base.transform.Rotate(isUsing ? usingRotationOffset : itemProperties.rotationOffset);
                base.transform.position = parentObject.position;
                Vector3 positionOffset = isUsing ? usingPositionOffset : itemProperties.positionOffset;
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

        public override void ItemActivate(bool used, bool buttonDown = true) // SYNCED
        {
            base.ItemActivate(used, buttonDown);

            isUsing = buttonDown;

            if (isBurning)
            {
                if (isUsing)
                    particleSystem.Stop();
                else
                    particleSystem.Play();
            }

            if (base.IsOwner)
            {
                playerHeldBy.activatingItem = isUsing;
                playerHeldBy.playerBodyAnimator.SetBool("useTZPItem", isUsing);

                if (timeInhaling > 0 && !isUsing)
                {
                    playerHeldBy.itemAudio.PlayOneShot(exhaleSFX, 1f);
                    StartCoroutine(EmitGas(timeInhaling));
                    timeInhaling = 0f;
                }
            }
        }

        public override void ChargeBatteries()
        {
            if (playerHeldBy == null) { return; }
            hasBeenLit = true;
            particleSystem.Play();
            audioSource.Play();
        }

        IEnumerator EmitGas(float time)
        {
            yield return null;
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", true);
            yield return new WaitForSeconds(time);
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
            if (playerHeldBy != null)
            {
                previousPlayerHeldBy = playerHeldBy;
            }
        }

        public override void DiscardItem() // SYNCED
        {
            if (previousPlayerHeldBy == localPlayer)
            {
                previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", value: false);
                previousPlayerHeldBy.activatingItem = false;

                if (timeInhaling > 0)
                {
                    playerHeldBy.itemAudio.PlayOneShot(exhaleSFX, 0.1f);
                    StartCoroutine(EmitGas(timeInhaling));
                    timeInhaling = 0f;
                }
            }

            base.DiscardItem();
        }
    }
}
