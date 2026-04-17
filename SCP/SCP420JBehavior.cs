using System;
using System.Collections.Generic;
using System.Text;

namespace ItemSCPs.SCP
{
    internal class SCP420JBehavior : PhysicsProp
    {
        bool usingItem;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            playerHeldBy.activatingItem = buttonDown;
            playerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
            usingItem = buttonDown;
        }
    }
}
