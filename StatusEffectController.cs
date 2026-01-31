using Unity.Netcode;

namespace ItemSCPs
{
    internal class StatusEffectController : NetworkBehaviour
    {
        public void Bleed()
        {
            /*if (localPlayer.criticallyInjured && timeSinceDamagePlayer > bleedDamageInterval)
            {
                timeSinceDamagePlayer = 0f;
                localPlayer.inSpecialInteractAnimation = true;
                localPlayer.DamagePlayer(bleedDamage, hasDamageSFX: false, causeOfDeath: CauseOfDeath.Stabbing);
                localPlayer.inSpecialInteractAnimation = false;
            }*/
        }


    }
}
