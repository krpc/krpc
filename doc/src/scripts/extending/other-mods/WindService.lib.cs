using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace Extending
{
    // [docs]
    sealed class WindAPI : ModAPI
    {
        public WindAPI () : base ("Wind", "WindMod")
        {
        }

        public static Func<Vessel, double> Speed;
        static Func<object> manager;

        protected override void Bind (ModBinder mod)
        {
            Speed = mod.Type ("WindMod.API").Method<Func<Vessel, double>> ("GetSpeed");
            manager = mod.Type ("WindMod.WindManager").StaticGetter<object> ("Instance");
        }

        protected override string NotReady ()
        {
            return manager () == null ? "not running in this scene" : null;
        }
    }

    [KRPCService (GameScene = GameScene.Flight)]
    [RequiresMod (typeof (WindAPI))]
    public static class Wind
    {
        [KRPCProperty (GameScene = GameScene.All)]
        public static bool Available {
            get { return ModAPI.Get<WindAPI> ().IsAvailable; }
        }

        [KRPCProperty]
        public static double Speed {
            get { return WindAPI.Speed (FlightGlobals.ActiveVessel); }
        }
    }
    // [/docs]
}
