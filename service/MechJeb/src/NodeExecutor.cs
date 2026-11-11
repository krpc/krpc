using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The maneuver node executor.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class NodeExecutor : ComputerModule
    {
        static Value<bool> autowarp;
        static Value<double> leadTime;
        static Action<object, object> executeOneNode;
        static Action<object, object> executeAllNodes;
        static Action<object> abort;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleNodeExecutor");
            autowarp = Reflection.Field<bool> (type, "Autowarp");
            leadTime = Reflection.EditableDouble (type, "LeadTime");
            executeOneNode = Reflection.Method<Action<object, object>> (type, "ExecuteOneNode");
            executeAllNodes = Reflection.Method<Action<object, object>> (type, "ExecuteAllNodes");
            abort = Reflection.Method<Action<object>> (type, "Abort");
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
        /// Execute the next maneuver node.
        /// </summary>
        [KRPCMethod]
        public void ExecuteOneNode ()
        {
            executeOneNode (Instance, this);
        }

        /// <summary>
        /// Execute all the maneuver nodes.
        /// </summary>
        [KRPCMethod]
        public void ExecuteAllNodes ()
        {
            executeAllNodes (Instance, this);
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
