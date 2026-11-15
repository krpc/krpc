using System;
using KRPC.Utils;

namespace KRPC.MechJeb
{
    /// <summary>
    /// MechJeb's maneuver node executor, which the maneuver planner's window controls.
    /// </summary>
    sealed class NodeExecutor : ComputerModule
    {
        static Field<object, bool> autowarp;
        static Field<object, double> leadTime;
        static Field<object, bool> rcsOnly;
        static Field<object, bool> killRollRotation;
        static Action<object, object> executeOneNode;
        static Action<object, object> executeAllNodes;
        static Action<object> abort;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.MechJebModuleNodeExecutor");
            autowarp = type.Field<object, bool> ("Autowarp");
            leadTime = Bindings.EditableDouble (type, "LeadTime");
            rcsOnly = type.Field<object, bool> ("RCSOnly");
            killRollRotation = type.Field<object, bool> ("KillRollRotation");
            executeOneNode = type.Method<Action<object, object>> ("ExecuteOneNode");
            executeAllNodes = type.Method<Action<object, object>> ("ExecuteAllNodes");
            abort = type.Method<Action<object>> ("Abort");
        }

        static readonly NodeExecutor executor = new NodeExecutor ();

        NodeExecutor () : base ("MechJebModuleNodeExecutor")
        {
        }

        /// <summary>
        /// MechJeb's setting to warp time, which every autopilot reads.
        /// </summary>
        internal static bool Autowarp {
            get { return autowarp.Get (executor.Instance); }
            set { autowarp.Set (executor.Instance, value); }
        }

        internal static double LeadTime {
            get { return leadTime.Get (executor.Instance); }
            set { leadTime.Set (executor.Instance, value); }
        }

        internal static bool RCSOnly {
            get { return rcsOnly.Get (executor.Instance); }
            set { rcsOnly.Set (executor.Instance, value); }
        }

        internal static bool KillRollRotation {
            get { return killRollRotation.Get (executor.Instance); }
            set { killRollRotation.Set (executor.Instance, value); }
        }

        internal static bool Executing {
            get { return executor.ModuleEnabled; }
        }

        internal static void ExecuteNextNode ()
        {
            CheckNodes ();
            executeOneNode (executor.Instance, executor);
        }

        internal static void ExecuteAllNodes ()
        {
            CheckNodes ();
            executeAllNodes (executor.Instance, executor);
        }

        internal static void Abort ()
        {
            abort (executor.Instance);
        }

        // MechJeb enables the executor before it reads the first node
        static void CheckNodes ()
        {
            var solver = FlightGlobals.ActiveVessel.patchedConicSolver;
            if (solver == null || solver.maneuverNodes.Count == 0)
                throw new InvalidOperationException ("The vessel has no maneuver node");
        }
    }
}
