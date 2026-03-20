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
            new Disease("Common Cold", () =>
            {
                // Decreased movement speed
                // Occasional sneezing that interrupts actions
                // Reduced stamina
                float time = UnityEngine.Random.Range(1200, 1800);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 250), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("sneeze", 0, 0.5f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
                }, "sneeze", time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "sprintMeter"));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1.8f, 2.5f, time, "sprintMultiplier"));
            }),

            new Disease("Flu", () =>
            {
                // Reduced health regeneration
                // Periodic coughing fits that make noise
                float time = UnityEngine.Random.Range(1800, 3000);
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect((x) => localPlayer.healthRegenerateTimer = 1f, "healthRegenerateTimer", duration: time));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 120), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("coughHeavy", 0, 0.6f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
                }, "coughHeavy", time));
            }),

            new Disease("Food Poisoning", () =>
            {
                // Decreased stamina
                // Random vomiting that interrupts actions
                // Health degeneration over time
                float time = UnityEngine.Random.Range(300, 1200);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "sprintMeter", true, true));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 300), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("puke", 0, 0.6f, 1, 5, 1500);
                    localPlayer.playQuickSpecialAnimation(2f);
                    localPlayer.playerBodyAnimator.SetTrigger("SpawnPlayer");
                    SpawnPuke(ItemSCPsContentHandler.Instance.SCP1025.PukeSplatterDecal, Utils.GetFloorPosition(localPlayer.bodyParts[0].position), Vector3.up); // TODO: Test this
                }, "puke", time));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 400), () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, false);
                    localPlayer.inSpecialInteractAnimation = false;
                }, duration: time));
            }),

            new Disease("Malaria", () =>
            {
                // Periodic high fevers causing temporary disorientation
                // Reduced stamina
                float time = UnityEngine.Random.Range(3000, 4800);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalPhaseActionEffect(new BoundedRange(300, 1000), new BoundedRange(30, 60), () => localPlayer.drunkness = Mathf.Max(localPlayer.drunkness, 0.3f), "migraine", time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.55f, 1f, time, "sprintMeter"));
            }),

            new Disease("Chickenpox", () =>
            {
                // Reduced stamina
                // Itchy skin causing random interruptions
                // Minor health degeneration
                float time = UnityEngine.Random.Range(1800, 3000);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.5f, 1f, time, "sprintMeter"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 200), () =>
                {
                    localPlayer.playQuickSpecialAnimation(2f);
                    localPlayer.playerBodyAnimator.SetTrigger("Overheat");
                }, "itch", time));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(120, 500), () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(2, false);
                    localPlayer.inSpecialInteractAnimation = false;
                }, duration: time));
            }),

            new Disease("Measles", () =>
            {
                // Decreased vision clarity
                // Reduced stamina
                // Health degeneration over time
                float time = UnityEngine.Random.Range(1800, 2400);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => StatusEffectController.Instance.vignetteOverlay.SetIntensity(x), 0f, 1f, time, "vignette"));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.drunkness = Mathf.Max(localPlayer.drunkness, x), 0, 0.4f, time, "drunkness"));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.6f, 1f, time, "sprintMeter"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 90), () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, false);
                    localPlayer.inSpecialInteractAnimation = false;
                }, duration: time));
            }),

            new Disease("Tuberculosis", () =>
            {
                // Severe coughing fits causing noise
                // Reduced stamina
                // Health degeneration over time
                float time = UnityEngine.Random.Range(4800, 7200);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 120), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("coughHeavy", 0, 0.6f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
                }, "coughHeavy", duration: time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.6f, 1f, time, "sprintMeter"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 90), () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, false);
                    localPlayer.inSpecialInteractAnimation = false;
                }, duration: time));
            }),

            new Disease("Bronchitis", () =>
            {
                // Persistent coughing causing noise
                // Reduced stamina
                // Minor health regeneration penalty
                float time = UnityEngine.Random.Range(2400, 3600);
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(10, 30), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("cough", 0, 0.5f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("ShortFallLanding");
                }, "cough", duration: time));
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0f, x), 0.7f, 1f, time, "sprintMeter"));
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect((x) => localPlayer.healthRegenerateTimer = 1f, "healthRegenerateTimer", duration: time));
            }),

            new Disease("Hypertension", () =>
            {
                // Increased damage taken from physical exertion
                // Periodic dizziness
                // Reduced stamina
                StatusEffectController.Instance.ApplyEffect(new ConditionalActionEffect(() => localPlayer.sprintMeter < 0.2f, () =>
                {
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(1, false);
                    localPlayer.inSpecialInteractAnimation = false;
                    localPlayer.drunkness = 0.2f;
                }, false, 1));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(60, 90), () => localPlayer.drunkness = 0.2f, "dizziness"));
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect((x) => localPlayer.sprintMeter = Mathf.Clamp(localPlayer.sprintMeter, 0, 0.75f), "sprintMeter"));
            }),

            new Disease("Pneumonia", () =>
            {
                // Decreased movement speed
                // Severe coughing fits causing noise
                // Significant health regeneration penalty
                float time = UnityEngine.Random.Range(3000, 4200);
                StatusEffectController.Instance.ApplyEffect(new LerpValueEffect((x) => localPlayer.sprintMultiplier = Mathf.Clamp(localPlayer.sprintMultiplier, 0f, x), 1.5f, 2.5f, time, "sprintMultiplier"));
                StatusEffectController.Instance.ApplyEffect(new RandomIntervalActionEffect(new BoundedRange(30, 90), () =>
                {
                    StatusEffectController.Instance.PlayRandomClipServerRpc("coughHeavy", 0, 0.75f, 1, 10, 1500);
                    localPlayer.playQuickSpecialAnimation(1f);
                    localPlayer.playerBodyAnimator.SetTrigger("SA_PushLeverBack");
                    localPlayer.inSpecialInteractAnimation = true;
                    localPlayer.DamagePlayer(2, false);
                    localPlayer.inSpecialInteractAnimation = false;
                    localPlayer.drunkness = 0.2f;
                }, "coughHeavy", duration: time));
            }),

            new Disease("Migraine", () =>
            {
                // Decreased vision clarity
                // Random severe headaches causing temporary disorientation
                // Reduced stamina
                float time = UnityEngine.Random.Range(300, 600);
                StatusEffectController.Instance.ApplyEffect(new TickActionEffect((x) => localPlayer.drunkness = Mathf.Max(localPlayer.drunkness, 0.15f), ));
            }),

            new Disease("Appendicitis", () =>
            {
                // Severe pain causing random interruptions
                // Reduced movement speed
                // Health degeneration requiring "surgery" item
                float time = UnityEngine.Random.Range(600, 1200);
            }),

            new Disease("Sinus Infection", () =>
            {
                // Reduced vision clarity
                // Persistent headache causing random interruptions
                // Reduced stamina
                float time = UnityEngine.Random.Range(1200, 2400);
            }),
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
    }

    [Serializable]
    public class Disease(string name, Action action)
    {
        public string name = name;
        public Action action = action;
    }
}

/*
Common Cold

    Status Effects: Decreased movement speed, occasional sneezing that interrupts actions, reduced stamina.

Flu

    Status Effects: Reduced health regeneration, periodic coughing fits that make noise, slight reduction in strength.

Food Poisoning

    Status Effects: Decreased stamina, random vomiting that interrupts actions, health degeneration over time.

Malaria

    Status Effects: Periodic high fevers causing temporary disorientation, reduced stamina, increased need for hydration.

Chickenpox

    Status Effects: Reduced stamina, itchy skin causing random interruptions, minor health degeneration.

Measles

    Status Effects: Decreased vision clarity, reduced stamina, health degeneration over time.

Tuberculosis

    Status Effects: Severe coughing fits causing noise, reduced stamina, health degeneration over time.

Asthma

    Status Effects: Reduced stamina, random asthma attacks causing temporary inability to move, need for "inhaler" item.

Bronchitis

    Status Effects: Persistent coughing causing noise, reduced stamina, minor health regeneration penalty.

Diabetes

    Status Effects: Need for "insulin" item, reduced stamina if not managed, occasional fatigue.

Hypertension

    Status Effects: Increased damage taken from physical exertion, periodic dizziness, reduced stamina.

Pneumonia

    Status Effects: Decreased movement speed, severe coughing fits causing noise, significant health regeneration penalty.

Migraine

    Status Effects: Decreased vision clarity, random severe headaches causing temporary disorientation, reduced stamina.

Appendicitis

    Status Effects: Severe pain causing random interruptions, reduced movement speed, health degeneration requiring "surgery" item.

Sinus Infection

    Status Effects: Reduced vision clarity, persistent headache causing random interruptions, reduced stamina.
*/