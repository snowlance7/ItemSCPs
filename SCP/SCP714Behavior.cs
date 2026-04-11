using System;
using System.Collections.Generic;
using System.Text;
using WearableItemsAPI;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP714Behavior : WearableObject
    {
        public static bool localPlayerAffected;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }


        }
    }
}
