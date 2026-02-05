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
}

/* use coroutines for these, make a manager
Cold

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