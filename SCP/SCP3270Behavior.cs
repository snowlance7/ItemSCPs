using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;


namespace ItemSCPs.SCP
{
    internal class SCP3270Behavior : PhysicsProp
    {
        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public GameObject catTail;
        public GameObject catEars;

        public Animation flipOnce;
        public Animation flipTwice;
        public bool flipped;
        public bool doneYet;

        public Animator animator;

        public override void Start()
        {
            animator = GetComponent<Animator>();
            flipped = false;
            doneYet = true;
            base.Start();
        }
        public override void Update()
        {
            base.Update();
            foreach (var player in StartOfRound.Instance.allPlayerScripts.Where(x => x.isPlayerControlled))
            {
                if (player.HasLineOfSightToPosition(transform.position, 10f, 1 * 2))
                {
                    //can it be a specific hitbox that I want that is on the object? prolly jsut need to get component
                    //need to make it so it is RPC and spawns the prefabs on the people
                }
            }

        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (doneYet && flipped)
            {
                flipOnce.Play(); //need to do animation and animator controller
            }
            else
            {
                flipTwice.Play();
            }
        }


    }


}

