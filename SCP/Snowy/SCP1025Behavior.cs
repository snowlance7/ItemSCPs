using BepInEx.Logging;
using Dawn.Utils;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;

namespace ItemSCPs.Items.Snowy
{
    internal class SCP1025Behavior : PhysicsProp // TODO: Make this work with SCP-714
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
        readonly Disease[] diseases = new Disease[]
        {
            new Disease("Cardiac Arrest", () =>
            {
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 60), () =>
                {
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.4f);
                    StatusEffectController.Instance.PlayLocalRandomClip("heartbeatSlow", 0, 0.7f, audibleNoiseID: -1);
                }));
                StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                {
                    StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        if (!localPlayer.isPlayerDead)
                            localPlayer.KillPlayer(Vector3.zero);
                    }, "cardiac arrest", 6f));
                    StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => StatusEffectController.Instance.vignetteOverlay.SetIntensity(x), 0.1f, 1f, 5f, "cardiac arrest"));
                    StatusEffectController.Instance.PlayLocalRandomClip("heartbeatFast", 0, audibleNoiseID: -1);
                    localPlayer.MakeCriticallyInjured(true);
                    localPlayer.sprintMeter = 0;
                }, "cardiac arrest", 500));
            }),

            new Disease("Common Cold", () =>
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
                }, "sneeze", time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1.7f, 2.5f, time, "sprintMultiplier"));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "sprintMeter"));
            }),

            new Disease("Chickenpox", () =>
            {
                // Reduced stamina
                // Itchy skin causing random interruptions
                // Minor health degeneration
                float time = UnityEngine.Random.Range(1800, 3000);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "sprintMeter"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("Overheat");
                }, "itch", time));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 200), () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, false);
                    localPlayer.inSpecialInteractAnimation = false;
                }, duration: time));
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect((x) => localPlayer.healthRegenerateTimer = 1, "healthDegeneration", time));
            }),

            new Disease("Cancer of the Lungs", () =>
            {
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 120), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1.5f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_Typing");
                    StatusEffectController.Instance.PlayRandomClipServerRpc("coughHeavy", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.2f);
                }, "coughHeavy"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(15, 40), () =>
                {
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("ShortFallLanding");
                    StatusEffectController.Instance.PlayRandomClipServerRpc("cough", 0, 0.6f, cutoffFrequency: 1500);
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(0.05f);
                }, "cough"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(120, 200), () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, false);
                    localPlayer.inSpecialInteractAnimation = false;
                }));
            }),

            new Disease("Appendicitis", () =>
            {
                // Severe pain causing random interruptions
                // Reduced movement speed
                float time = UnityEngine.Random.Range(600, 1200);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 200), () =>
                {
                    localPlayer.playQuickSpecialAnimation(2f);
                    localPlayer.playerBodyAnimator.SetTrigger("Overheat");
                    StatusEffectController.Instance.PlayLocalRandomClip("pain", 0, 0.5f, 1, 5, 1500);
                }, "pain", time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1f, 2.5f, time, "sprintMultiplier"));
            }),

            new Disease("Asthma", () =>
            {
                // TODO
            })
        };

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
        }
    }

    [Serializable]
    public class Disease(string name, Action action)
    {
        public string name = name;
        public Action action = action;
    }
}