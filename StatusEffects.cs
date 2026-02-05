using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public class BleedingEffect(int amount, float interval, int damage) : StatusEffect
    {
        public override bool AllowMultipleInstances => true;

        int amount = amount;
        float interval = interval;
        int damage = damage;

        Coroutine? bleedRoutine;

        public override void OnApply()
        {
            IEnumerator BleedRoutine(int amount, float interval, int damage)
            {
                yield return null;

                for (int i = 0; i < amount; i++)
                {
                    yield return new WaitForSeconds(interval);
                    localPlayer.DropBlood();
                    if (damage > 0)
                        localPlayer.DamagePlayer(damage);
                }

                bleedRoutine = null;
                controller.RemoveEffect(this);
            }

            bleedRoutine = controller.StartCoroutine(BleedRoutine(amount, interval, damage));
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (bleedRoutine != null)
                controller.StopCoroutine(bleedRoutine);
        }
    }


}
