using Dusk;
using UnityEngine;

namespace ItemSCPs
{
    public class ItemSCPsContentHandler : ContentHandler<ItemSCPsContentHandler>
    {
        public class NetworkHandlerAssets(DuskMod mod, string filePath) : AssetBundleLoader<NetworkHandlerAssets>(mod, filePath)
        {
            [LoadFromBundle("ItemSCPsNetworkHandler.prefab")]
            public GameObject NetworkHandlerPrefab { get; private set; } = null!;
        }
        public NetworkHandlerAssets? NetworkHandler;

        // Rat
        /*
        public class SCP018Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP018Assets>(mod, filePath) { }
        public SCP018Assets? SCP018;

        public class SCP207Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP207Assets>(mod, filePath) { }
        public SCP207Assets? SCP207;

        public class SCP005Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP005Assets>(mod, filePath) { }
        public SCP005Assets? SCP005;

        public class SCP201Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP201Assets>(mod, filePath) { }
        public SCP201Assets? SCP201;

        public class SCP3270Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP3270Assets>(mod, filePath) { }
        public SCP3270Assets? SCP3270;

        public class SCP714Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP714Assets>(mod, filePath) { }
        public SCP714Assets? SCP714;
        */
        // Snowy
        public class SCP1079Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP1079Assets>(mod, filePath)
        {
            [LoadFromBundle("PinkBloodDecal.mat")]
            public Material PinkBloodDecal { get; private set; } = null!;

            [LoadFromBundle("PinkFootprintsDecal.mat")]
            public Material PinkFootprintsDecal { get; private set; } = null!;
        }
        public SCP1079Assets? SCP1079;

        public class SCP983Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP983Assets>(mod, filePath) { }
        public SCP983Assets? SCP983;

        public class SCP012Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP012Assets>(mod, filePath) { }
        public SCP012Assets? SCP012;

        public class SCP1025Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP1025Assets>(mod, filePath) { }
        public SCP1025Assets? SCP1025;

        public class SCP268Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP268Assets>(mod, filePath) { }
        public SCP268Assets? SCP268;

        public class SCP3482Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP3482Assets>(mod, filePath) { }
        public SCP3482Assets? SCP3482;

        public class SCP420JAssets(DuskMod mod, string filePath) : AssetBundleLoader<SCP420JAssets>(mod, filePath) { }
        public SCP420JAssets? SCP420J;

        public class SCP498Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP498Assets>(mod, filePath) { }
        public SCP498Assets? SCP498;

        public class SCP735Assets(DuskMod mod, string filePath) : AssetBundleLoader<SCP735Assets>(mod, filePath) { }
        public SCP735Assets? SCP735;

        public ItemSCPsContentHandler(DuskMod mod) : base(mod)
        {
            RegisterContent("networkhandler", out NetworkHandler);

            // Rat
            /*RegisterContent("scp018", out SCP018);
            RegisterContent("scp207", out SCP207);
            RegisterContent("scp005", out SCP005);
            RegisterContent("scp201", out SCP201);
            RegisterContent("scp3270", out SCP3270);
            RegisterContent("scp714", out SCP714);*/

            // Snowy
            RegisterContent("scp012", out SCP012);
            //RegisterContent("scp1025", out SCP1025);
            //RegisterContent("scp1079", out SCP1079);
            //RegisterContent("scp268", out SCP268);
            //RegisterContent("scp3482", out SCP3482);
            //RegisterContent("scp420J", out SCP420J);
            //RegisterContent("scp498", out SCP498);
            //RegisterContent("scp735", out SCP735);
            RegisterContent("scp983", out SCP983);
        }
    }

}

/*public class ScrapDroneAssets(DuskMod mod, string filePath) : AssetBundleLoader<ScrapDroneAssets>(mod, filePath)
{
    [LoadFromBundle("ScrapDrone.prefab")]
    public GameObject ScrapDronePrefab { get; private set; } = null!;
}*/