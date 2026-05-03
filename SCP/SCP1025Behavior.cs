using Dawn.Utils;
using SnowyLib;
using System;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP1025Behavior : PhysicsProp
    {
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
                localPlayer.StatusEffectController().ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 200), () =>
                {
                    ItemSCPsNetworkHandler.Instance.PlayPlayerSoundEffectServerRpc(localPlayer.actualClientId, ItemSCPsNetworkHandler.SoundEffect.Sneeze, 0, 0.5f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
                }, "scp1025", "sneeze", time, onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                localPlayer.StatusEffectController().ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1.7f, 2.5f, time, "scp1025", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                localPlayer.StatusEffectController().ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "scp1025", "sprintMeter", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 1 Chickenpox
            () =>
            {
                // Reduced stamina
                // Itchy skin causing random interruptions
                // Minor health degeneration
                float time = UnityEngine.Random.Range(1800, 3000);
                localPlayer.StatusEffectController().ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "scp1025", "sprintMeter", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                localPlayer.StatusEffectController().ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                        localPlayer.DamagePlayer(1, false);
                }, "scp1025", "chickenpox itch", time));
                localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() => localPlayer.healthRegenerateTimer = 1, "scp1025", "healthRegenerateTimer", time, onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 2 Cancer of the Lungs
            () =>
            {
                localPlayer.StatusEffectController().ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1.5f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_Typing");
                    ItemSCPsNetworkHandler.Instance.PlayPlayerSoundEffectServerRpc(localPlayer.actualClientId, ItemSCPsNetworkHandler.SoundEffect.CoughHeavy, 0, 0.5f, cutoffFrequency: 1500);
                    VignetteOverlay.Instance.SetIntensity(0.2f);
                    if (localPlayer.health > 1)
                    {
                        localPlayer.inSpecialInteractAnimation = true;
                        localPlayer.DamagePlayer(1, false);
                        localPlayer.inSpecialInteractAnimation = false;
                    }
                }, "scp1025", "lung cancer coughHeavy", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                localPlayer.StatusEffectController().ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(15, 40), () =>
                {
                    ItemSCPsNetworkHandler.Instance.PlayPlayerSoundEffectServerRpc(localPlayer.actualClientId, ItemSCPsNetworkHandler.SoundEffect.Cough, 0, 0.5f, cutoffFrequency: 1500);
                    VignetteOverlay.Instance.SetIntensity(0.05f);
                }, "scp1025", "lung cancer cough", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 3 Appendicitis
            () =>
            {
                // Severe pain causing random interruptions
                // Reduced movement speed
                float time = UnityEngine.Random.Range(600, 1200);
                localPlayer.StatusEffectController().ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 200), () => localPlayer.DamagePlayer(1), "scp1025", "appendicitis pain", time));
                localPlayer.StatusEffectController().ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1f, 2.5f, time, "scp1025", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
            },
            // 4 Asthma
            () =>
            {
                localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() =>
                {
                    float cap = Mathf.Lerp(1f, 2.5f, localPlayer.sprintMeter);
                    localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0, cap);
                }, "scp1025", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft && incoming.source == existing.source ? StatusEffectController.ConflictResult.Replace : StatusEffectController.ConflictResult.Deny));
                localPlayer.StatusEffectController().ApplyEffect(new ConditionalActionEffect(() => localPlayer.sprintMeter < 0.5f, () =>
                {
                    if (UnityEngine.Random.Range(0, 2) == 0) { return; }
                    ItemSCPsNetworkHandler.Instance.PlayPlayerSoundEffectServerRpc(localPlayer.actualClientId, ItemSCPsNetworkHandler.SoundEffect.Cough, 0, 0.5f, cutoffFrequency: 1500);
                    VignetteOverlay.Instance.SetIntensity(0.05f);
                }, false, "scp1025", 5f, id: "asthmaCough"));
            },
            // 5 Cardiac Arrest
            () =>
            {
                localPlayer.StatusEffectController().ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(10, 20), () =>
                {
                    VignetteOverlay.Instance.SetIntensity(0.4f);
                    Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], ItemSCPsNetworkHandler.Instance.heartbeatSlowSFX, 0.7f, audibleNoiseID: -1);
                }, "scp1025", "heartbeatSlow"));
                localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        if (!localPlayer.isPlayerDead)
                            localPlayer.KillPlayer(Vector3.zero);
                    }, "scp1025", "heart attack", 6f));
                    localPlayer.StatusEffectController().ApplyEffect(new LerpValueEffect((x) => VignetteOverlay.Instance.SetIntensity(x), 0.1f, 1f, 5f, "scp1025", "vignette"));
                    Utils.PlaySoundAtPosition(localPlayer.bodyParts[0], ItemSCPsNetworkHandler.Instance.heartbeatFastSFX, audibleNoiseID: -1);
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
            itemProperties.toolTips = new string[] { "Open Book [LMB]" };
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if (TESTING.immunity || SCP714Behavior.localPlayerAffected) { return; }
            if (UnityEngine.Random.Range(0f, 1f) < openBookChance)
            {
                int index = UnityEngine.Random.Range(0, diseases.Length);
                OpenBookServerRpc(index);
                diseases[index].Invoke();
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            int index = UnityEngine.Random.Range(0, diseases.Length);
            OpenBookServerRpc(index);
            diseases[index].Invoke();
        }

        public override void DiscardItem()
        {
            base.DiscardItem();
            CloseBookServerRpc();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            CloseBookServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void CloseBookServerRpc()
        {
            if (!IsServer) { return; }
            CloseBookClientRpc();
        }

        [ClientRpc]
        public void CloseBookClientRpc()
        {
            animator.SetBool("open", false);
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
            var mats = renderer.materials;
            mats[2] = diseasePageMaterials[pageIndex];
            renderer.materials = mats;
            animator.SetBool("open", true);
        }
    }
}