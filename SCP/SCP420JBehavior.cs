using GameNetcodeStuff;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ItemSCPs.SCP
{
    public class SCP420JBehavior : PhysicsProp // TODO
    {
        public AudioSource audioSource = null!;
        public ParticleSystem particleSystem = null!;

        PlayerControllerB? previousPlayerHeldBy;

        Coroutine? useBluntCoroutine;

        bool emittingGas;

        float fuel = 1f;

        bool triedUsingWithoutFuel;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                isBeingUsed = true;
                if (fuel <= 0f)
                {
                    if (!triedUsingWithoutFuel)
                    {
                        triedUsingWithoutFuel = true;
                    }
                    return;
                }
                previousPlayerHeldBy = playerHeldBy;
                useBluntCoroutine = StartCoroutine(UseTZPAnimation());
            }
            else
            {
                isBeingUsed = false;
                if (triedUsingWithoutFuel)
                {
                    triedUsingWithoutFuel = false;
                }
                else if (useBluntCoroutine != null)
                {
                    StopCoroutine(useBluntCoroutine);
                    emittingGas = false;
                    previousPlayerHeldBy.activatingItem = false;
                    audioSource.Stop();
                    //localHelmetSFX.Stop();
                    //audioSource.PlayOneShot(removeCanSFX);
                }
            }
            if (base.IsOwner)
            {
                previousPlayerHeldBy.activatingItem = buttonDown;
                previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
            }
        }

        IEnumerator UseTZPAnimation()
        {
            //audioSource.PlayOneShot(holdCanSFX);
            //WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, holdCanSFX);
            yield return new WaitForSeconds(0.75f);
            emittingGas = true;
            HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: true);
            if (base.IsOwner)
            {
                //localHelmetSFX.Play();
                //localHelmetSFX.PlayOneShot(twistCanSFX);
            }
            else
            {
                //audioSource.clip = releaseGasSFX;
                audioSource.Play();
                //audioSource.PlayOneShot(twistCanSFX);
            }
            //WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, twistCanSFX);
        }

        public override void Update()
        {
            if (emittingGas)
            {
                if (previousPlayerHeldBy == null || !isHeld || fuel <= 0f)
                {
                    emittingGas = false;
                    audioSource.Stop();
                    //localHelmetSFX.Stop();
                    RunOutOfFuelServerRpc();
                }
                previousPlayerHeldBy.drunknessInertia = Mathf.Clamp(previousPlayerHeldBy.drunknessInertia + Time.deltaTime / 1.75f * previousPlayerHeldBy.drunknessSpeed, 0.1f, 3f);
                previousPlayerHeldBy.increasingDrunknessThisFrame = true;
                fuel -= Time.deltaTime / 38f;
                previousPlayerHeldBy.sprintMeter = Mathf.Clamp(previousPlayerHeldBy.sprintMeter + Time.deltaTime / (previousPlayerHeldBy.sprintTime + 9f), 0f, 1f);
            }
            base.Update();
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

        [ServerRpc]
        public void RunOutOfFuelServerRpc()
        {
            if (!IsServer) { return; }
            RunOutOfFuelClientRpc();
        }

        [ClientRpc]
        public void RunOutOfFuelClientRpc()
        {
            itemUsedUp = true;
            emittingGas = false;
            fuel = 0f;
            audioSource.Stop();
            //localHelmetSFX.Stop();
        }

        public override void DiscardItem()
        {
            emittingGas = false;
            audioSource.Stop();
            //localHelmetSFX.Stop();
            //playerHeldBy.playerBodyAnimator.ResetTrigger("shakeItem");
            previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", value: false);
            if (previousPlayerHeldBy != null)
            {
                previousPlayerHeldBy.activatingItem = false;
            }
            base.DiscardItem();
        }
    }
}
