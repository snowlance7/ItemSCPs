using Dawn.Utils;
using ItemSCPs.SCP.Snowy;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;

namespace ItemSCPs.Items.Snowy
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

        readonly Action[] diseases = new Action[]
        {
            // Common Cold
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
                }, "Common Cold", "sneeze", time, onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1.7f, 2.5f, time, "Common Cold", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "Common Cold", "sprintMeter", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
            },
            // Chickenpox
            () =>
            {
                // Reduced stamina
                // Itchy skin causing random interruptions
                // Minor health degeneration
                float time = UnityEngine.Random.Range(1800, 3000);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "Chickenpox", "sprintMeter", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("Overheat");
                    if (localPlayer.health > 1)
                    {
                        localPlayer.inSpecialInteractAnimation = true;
                        localPlayer.DamagePlayer(1, false);
                        localPlayer.inSpecialInteractAnimation = false;
                    }
                }, "Chickenpox", "itch", time));
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect(() => localPlayer.healthRegenerateTimer = 1, "Chickenpox", "healthRegenerateTimer", time, onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
            },
            // Cancer of the Lungs
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
                }, "Cancer of the Lungs", "coughHeavy", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(15, 40), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("ShortFallLanding");
                    StatusEffectController.Instance.PlayRandomClipServerRpc("cough", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.05f);
                }, "Cancer of the Lungs", "cough", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
            },
            // Appendicitis
            () =>
            {
                // Severe pain causing random interruptions
                // Reduced movement speed
                float time = UnityEngine.Random.Range(600, 1200);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 200), () =>
                {
                    localPlayer.playQuickSpecialAnimation(2f);
                    localPlayer.playerBodyAnimator.SetTrigger("Overheat");
                    StatusEffectController.Instance.PlayLocalRandomClip("pain", 0, 0.5f, 1, 5, 1500);
                }, "Appendicitis", "pain", time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1f, 2.5f, time, "Appendicitis", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft));
            },
            // Asthma
            () =>
            {
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect(() =>
                {
                    float cap = Mathf.Lerp(1f, 2.5f, localPlayer.sprintMeter);
                    localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0, cap);
                }, "Asthma", "sprintMultiplier", onConflict: (existing, incoming) => incoming.duration > existing.timeLeft && incoming.source == existing.source));
                StatusEffectController.Instance.ApplyEffect(new ConditionalActionEffect(() => localPlayer.sprintMeter < 0.5f, () =>
                {
                    if (UnityEngine.Random.Range(0, 2) == 0) { return; }
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("ShortFallLanding");
                    StatusEffectController.Instance.PlayRandomClipServerRpc("cough", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.05f);
                }, false, "Asthma", 5f, id: "asthmaCough"));
            },
            // Cardiac Arrest
            () =>
            {
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 60), () =>
                {
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.4f);
                    StatusEffectController.Instance.PlayLocalRandomClip("heartbeatSlow", 0, 0.7f, audibleNoiseID: -1);
                }, "Cardiac Arrest", "heartbeatSlow", onConflict: (existing, incoming) => false));
                StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        if (!localPlayer.isPlayerDead)
                            localPlayer.KillPlayer(Vector3.zero);
                    }, "Cardiac Arrest", "Kill Player in 6 seconds", 6f));
                    StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => StatusEffectController.Instance.vignetteOverlay.SetIntensity(x), 0.1f, 1f, 5f, "Cardiac Arrest", "vignette increase"));
                    StatusEffectController.Instance.PlayLocalRandomClip("heartbeatFast", 0, audibleNoiseID: -1);
                    localPlayer.MakeCriticallyInjured(true);
                    localPlayer.bleedingHeavily = false;
                    localPlayer.sprintMeter = 0;
                }, "Cardiac Arrest", "Incoming Heart Attack", 500, onConflict: (existing, incoming) => false));
            }
        };

        // Configs
        const float openBookChance = 0.1f;

        public static void SpawnPuke(Material pukeMaterial, Vector3 position, Vector3 normal)
        {
            GameObject decalObj = new GameObject("PukeDecal");

            var projector = decalObj.AddComponent<DecalProjector>();
            projector.material = pukeMaterial;

            // Position slightly above surface to avoid z-fighting
            decalObj.transform.position = position + normal * 0.02f;

            // Rotate to match the ground
            decalObj.transform.rotation = Quaternion.LookRotation(-normal);

            // Size of the splat
            projector.size = new Vector3(1.5f, 1.5f, 1.5f);

            // Optional: random rotation for variation
            decalObj.transform.Rotate(Vector3.forward, UnityEngine.Random.Range(0f, 360f));

            Destroy(decalObj, 60f);
        }

        public override void EquipItem()
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