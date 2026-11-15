using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The maneuver node executor.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class NodeExecutor : ComputerModule
    {
        static Field<object, bool> autowarp;
        static Field<object, double> leadTime;
        static Action<object, object> executeOneNode;
        static Action<object, object> executeAllNodes;
        static Action<object> abort;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleNodeExecutor");
            autowarp = type.Field<object, bool> ("Autowarp");
            leadTime = Bindings.EditableDouble (type, "LeadTime");
            executeOneNode = type.Method<Action<object, object>> ("ExecuteOneNode");
            executeAllNodes = type.Method<Action<object, object>> ("ExecuteAllNodes");
            abort = type.Method<Action<object>> ("Abort");
        }

        internal NodeExecutor () : base ("MechJebModuleNodeExecutor")
        {
        }

        /// <summary>
        /// Whether the executor is running.
        /// </summary>
        [KRPCProperty]
        public override bool Enabled {
            get { return base.Enabled; }
        }

        /// <summary>
        /// Whether the executor warps to the burn.
        /// </summary>
        [KRPCProperty]
        public bool Autowarp {
            get { return autowarp.Get (Instance); }
            set { autowarp.Set (Instance, value); }
        }

        /// <summary>
        /// The time before the burn at which time warp stops, in seconds.
        /// </summary>
        [KRPCProperty]
        public double LeadTime {
            get { return leadTime.Get (Instance); }
            set { leadTime.Set (Instance, value); }
        }

        /// <summary>
        /// Execute the next maneuver node. Throws an exception if the vessel has no maneuver
        /// node.
        /// </summary>
        [KRPCMethod]
        public void ExecuteOneNode ()
        {
            CheckNodes ();
            executeOneNode (Instance, this);
        }

        /// <summary>
        /// Execute all the maneuver nodes. Throws an exception if the vessel has no maneuver
        /// node.
        /// </summary>
        [KRPCMethod]
        public void ExecuteAllNodes ()
        {
            CheckNodes ();
            executeAllNodes (Instance, this);
        }

        // MechJeb enables the executor before it reads the first node
        static void CheckNodes ()
        {
            var solver = FlightGlobals.ActiveVessel.patchedConicSolver;
            if (solver == null || solver.maneuverNodes.Count == 0)
                throw new InvalidOperationException ("The vessel has no maneuver node");
        }

        /// <summary>
        /// Stop executing the maneuver nodes.
        /// </summary>
        [KRPCMethod]
        public void Abort ()
        {
            abort (Instance);
        }
    }
}
