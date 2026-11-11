using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The controller of one kind of deployable part, such as antennas or solar panels.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class DeployableController : ComputerModule
    {
        static Value<bool> autoDeploy;
        static Action<object> extendAll;
        static Action<object> retractAll;
        static Func<object, bool> allRetracted;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleDeployableController");
            autoDeploy = Reflection.Field<bool> (type, "autoDeploy");
            extendAll = Reflection.Method<Action<object>> (type, "ExtendAll");
            retractAll = Reflection.Method<Action<object>> (type, "RetractAll");
            allRetracted = Reflection.Method<Func<object, bool>> (type, "AllRetracted");
        }

        internal DeployableController (string name) : base (name)
        {
        }

        /// <summary>
        /// Whether a MechJeb autopilot deploys the parts.
        /// </summary>
        [KRPCProperty]
        public bool AutoDeploy {
            get { return autoDeploy.Get (Instance); }
            set { autoDeploy.Set (Instance, value); }
        }

        /// <summary>
        /// Extend all the parts.
        /// </summary>
        [KRPCMethod]
        public void ExtendAll ()
        {
            extendAll (Instance);
        }

        /// <summary>
        /// Retract all the parts.
        /// </summary>
        [KRPCMethod]
        public void RetractAll ()
        {
            retractAll (Instance);
        }

        /// <summary>
        /// Whether all the parts are retracted.
        /// </summary>
        [KRPCMethod]
        public bool AllRetracted ()
        {
            return allRetracted (Instance);
        }
    }
}
