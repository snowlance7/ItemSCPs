using Dawn.Utils;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;
using SnowyLib;

namespace ItemSCPs.SCP
{
    internal class SCP1025Behavior : PhysicsProp
    {
        /*
        ShortFallLanding (Trigger) - coughing small motion
        SpawnPlayer (Trigger) - puking
        startCrouching (Trigger) - force crouch, specialanimation time for duration
        Damage (Trigger) - hands in air
        Overheat (Trigger) - hands in air lower
        SA_Typing (Trigger) - puking motion, head forward?
        SA_stopAnimation (Trigger)
        SA_ChargeItem (Trigger) - hand out
        SA_PushLeverBack (Trigger) - forces screen to middle and does quick animation
        */
        //localPlayer.sprintMeter 0-1
        //localPlayer.sprintTime 11, idk what this does
        //localPlayer.sprintMultiplier 1-2.5, controls sprint speed

#pragma warning disable CS8618
        public Animator animator;
        public Material[] diseasePageMaterials;
        public SkinnedMeshRenderer renderer;
#pragma warning restore CS8618

        public static readonly Action[] diseases = new Action[] // TODO: Test and rework these
        {
            // 0 Common Cold
            () =>
            {
                // Occasional sneezing that interrupts actions
                // Decreased movement speed
                // Reduced stamina
                float time = UnityEngine.Random.Range(1200, 1800);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 200), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("sneeze", 0, 0.5f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
                }, "scp1025", "sneeze", time, onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1.7f, 2.5f, time, "scp1025", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "scp1025", "sprintMeter", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 1 Chickenpox
            () =>
            {
                // Reduced stamina
                // Itchy skin causing random interruptions
                // Minor health degeneration
                float time = UnityEngine.Random.Range(1800, 3000);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "scp1025", "sprintMeter", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                        localPlayer.DamagePlayer(1, false);
                }, "scp1025", "chickenpox itch", time));
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect(() => localPlayer.healthRegenerateTimer = 1, "scp1025", "healthRegenerateTimer", time, onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 2 Cancer of the Lungs
            () =>
            {
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1.5f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_Typing");
                    StatusEffectController.Instance.PlayRandomClipServerRpc("coughHeavy", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.2f);
                    if (localPlayer.health > 1)
                    {
                        localPlayer.inSpecialInteractAnimation = true;
                        localPlayer.DamagePlayer(1, false);
                        localPlayer.inSpecialInteractAnimation = false;
                    }
                }, "scp1025", "lung cancer coughHeavy", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(15, 40), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("cough", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.05f);
                }, "scp1025", "lung cancer cough", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 3 Appendicitis
            () =>
            {
                // Severe pain causing random interruptions
                // Reduced movement speed
                float time = UnityEngine.Random.Range(600, 1200);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 200), () => localPlayer.DamagePlayer(1), "scp1025", "appendicitis pain", time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1f, 2.5f, time, "scp1025", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 4 Asthma
            () =>
            {
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect(() =>
                {
                    float cap = Mathf.Lerp(1f, 2.5f, localPlayer.sprintMeter);
                    localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0, cap);
                }, "scp1025", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft && incoming.source == existing.source ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                StatusEffectController.Instance.ApplyEffect(new ConditionalActionEffect(() => localPlayer.sprintMeter < 0.5f, () =>
                {
                    if (UnityEngine.Random.Range(0, 2) == 0) { return; }
                    StatusEffectController.Instance.PlayRandomClipServerRpc("cough", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.05f);
                }, false, "scp1025", 5f, id: "asthmaCough"));
            },
            // 5 Cardiac Arrest
            () =>
            {
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(10, 20), () =>
                {
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.4f);
                    StatusEffectController.Instance.PlayLocalRandomClip("heartbeatSlow", 0, 0.7f, audibleNoiseID: -1);
                }, "scp1025", "heartbeatSlow"));
                StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        if (!localPlayer.isPlayerDead)
                            localPlayer.KillPlayer(Vector3.zero);
                    }, "scp1025", "heart attack", 6f));
                    StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => StatusEffectController.Instance.vignetteOverlay.SetIntensity(x), 0.1f, 1f, 5f, "scp1025", "vignette"));
                    StatusEffectController.Instance.PlayLocalRandomClip("heartbeatFast", 0, audibleNoiseID: -1);
                    localPlayer.MakeCriticallyInjured(true);
                    localPlayer.bleedingHeavily = false;
                    localPlayer.sprintMeter = 0;
                }, "scp1025", "incoming heart attack", 60));
            }
        };

        // Configs
        const float openBookChance = 0.1f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.1f, 0.13f, -0.19f);
            itemProperties.rotationOffset = new Vector3(180, 90, 0);
            itemProperties.floorYOffset = 90;

            itemProperties.canBeInspected = true;
            itemProperties.canBeGrabbedBeforeGameStart = true;
        }

        public override void EquipItem() // TODO: Pages not showing
        {
            base.EquipItem();
            if (TESTING.localPlayerImmune || SCP500Behavior.localPlayerAffected || SCP714Behavior.localPlayerAffected) { return; }
            if (UnityEngine.Random.Range(0f, 1f) < openBookChance)
            {
                int index = UnityEngine.Random.Range(0, diseases.Length);
                OpenBookServerRpc(index);
                diseases[index].Invoke();
            }
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            OpenBookServerRpc(-1);
        }

        public override void PocketItem()
        {
            base.PocketItem();
            OpenBookServerRpc(-1);
        }

        [ServerRpc(RequireOwnership = false)]
        public void OpenBookServerRpc(int pageIndex)
        {
            if (!IsServer) { return; }
            OpenBookClientRpc(pageIndex);
        }

        [ClientRpc]
        public void OpenBookClientRpc(int pageIndex)
        {
            if (pageIndex < 0)
            {
                animator.SetBool("open", false);
                return;
            }
            renderer.materials[2] = diseasePageMaterials[pageIndex];
            animator.SetBool("open", true);
        }
    }
}