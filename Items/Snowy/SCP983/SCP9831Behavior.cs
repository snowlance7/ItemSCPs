using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.Items.Snowy.SCP983
{
    internal class SCP9831Behavior : PhysicsProp
    {
        public AudioClip EatCandySFX;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown)
            {
                playerHeldBy.statusEffectAudio.PlayOneShot(EatCandySFX, 1f);
                playerHeldBy.DespawnHeldObject();
            }
        }

        // RPCs
    }
}