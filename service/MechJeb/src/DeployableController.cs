using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The controller of one kind of deployable part, such as antennas or solar panels.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class DeployableController : ComputerModule
    {
        static Field<object, bool> autoDeploy;
        static Action<object> extendAll;
        static Action<object> retractAll;
        static Func<object, bool> allRetracted;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleDeployableController");
            autoDeploy = type.Field<object, bool> ("autoDeploy");
            extendAll = type.Method<Action<object>> ("ExtendAll");
            retractAll = type.Method<Action<object>> ("RetractAll");
            allRetracted = type.Method<Func<object, bool>> ("AllRetracted");
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
