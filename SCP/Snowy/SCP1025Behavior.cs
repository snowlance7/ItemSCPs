using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;
using System.Net;
using System;

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
        Disease[] diseases = new Disease[]
{
    new Disease("Common Cold", () =>
    {
        // Decreased movement speed
        // Occasional sneezing that interrupts actions
        // Reduced stamina
        StatusEffectController.Instance.ApplyEffect(new SprintSpeedCapEffect(1.8f, ))
    }),

    new Disease("Flu", () =>
    {
        // Reduced health regeneration
        // Periodic coughing fits that make noise
        // Slight reduction in strength
    }),

    new Disease("Food Poisoning", () =>
    {
        // Decreased stamina
        // Random vomiting that interrupts actions
        // Health degeneration over time
    }),

    new Disease("Malaria", () =>
    {
        // Periodic high fevers causing temporary disorientation
        // Reduced stamina
        // Increased need for hydration
    }),

    new Disease("Chickenpox", () =>
    {
        // Reduced stamina
        // Itchy skin causing random interruptions
        // Minor health degeneration
    }),

    new Disease("Measles", () =>
    {
        // Decreased vision clarity
        // Reduced stamina
        // Health degeneration over time
    }),

    new Disease("Tuberculosis", () =>
    {
        // Severe coughing fits causing noise
        // Reduced stamina
        // Health degeneration over time
    }),

    new Disease("Asthma", () =>
    {
        // Reduced stamina
        // Random asthma attacks causing temporary inability to move
        // Requires "inhaler" item
    }),

    new Disease("Bronchitis", () =>
    {
        // Persistent coughing causing noise
        // Reduced stamina
        // Minor health regeneration penalty
    }),

    new Disease("Diabetes", () =>
    {
        // Requires "insulin" item
        // Reduced stamina if not managed
        // Occasional fatigue
    }),

    new Disease("Hypertension", () =>
    {
        // Increased damage taken from physical exertion
        // Periodic dizziness
        // Reduced stamina
    }),

    new Disease("Pneumonia", () =>
    {
        // Decreased movement speed
        // Severe coughing fits causing noise
        // Significant health regeneration penalty
    }),

    new Disease("Migraine", () =>
    {
        // Decreased vision clarity
        // Random severe headaches causing temporary disorientation
        // Reduced stamina
    }),

    new Disease("Appendicitis", () =>
    {
        // Severe pain causing random interruptions
        // Reduced movement speed
        // Health degeneration requiring "surgery" item
    }),

    new Disease("Sinus Infection", () =>
    {
        // Reduced vision clarity
        // Persistent headache causing random interruptions
        // Reduced stamina
    }),
};

        public override void Start()
        {
            base.Start();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
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