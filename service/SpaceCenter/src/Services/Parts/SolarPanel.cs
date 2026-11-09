using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A solar panel. Obtained by calling <see cref="Part.SolarPanel"/>.
    /// </summary>
    /// <remarks>
    /// Includes the curved solar panels of Near Future Solar.
    /// </remarks>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class SolarPanel : Equatable<SolarPanel>, IGameObjectState
    {
        ModuleRef panelRef;
        // The Near Future Solar accessors, or null for a stock panel
        readonly CurvedSolarPanel curved;

        internal static bool Is (Part part)
        {
            var internalPart = part.InternalPart;
            return internalPart.HasModule<ModuleDeployableSolarPanel> () ||
                CurvedSolarPanel.Find (internalPart) != null;
        }

        internal SolarPanel (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            if (internalPart.HasModule<ModuleDeployableSolarPanel> ()) {
                panelRef = ModuleRef.ForType<ModuleDeployableSolarPanel> (internalPart);
                return;
            }
            var module = CurvedSolarPanel.Find (internalPart);
            if (module == null)
                throw new ArgumentException ("Part is not a solar panel");
            panelRef = ModuleRef.ForModule (module);
            curved = CurvedSolarPanel.For (module.GetType ());
        }

        global::PartModule InternalModule {
            get { return panelRef.Get (Part.InternalPart); }
        }

        ModuleDeployableSolarPanel InternalPanel {
            get { return (ModuleDeployableSolarPanel)InternalModule; }
        }

        ModuleDeployablePart.DeployState DeployState {
            get {
                if (curved == null)
                    return InternalPanel.deployState;
                var module = InternalModule;
                // A fixed curved panel keeps no deploy state
                return curved.Deployable (module) ? curved.State (module) : ModuleDeployablePart.DeployState.EXTENDED;
            }
        }

        // A curved panel keeps its last readings once it stops being extended
        bool Generating {
            get { return curved == null || DeployState == ModuleDeployablePart.DeployState.EXTENDED; }
        }

        /// <summary>
        /// The state of the part carrying the solar panel, or destroyed once that part
        /// loses the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return panelRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (SolarPanel other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && panelRef == other.panelRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (panelRef);
        }

        /// <summary>
        /// The part object for this solar panel.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the solar panel is deployable.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public bool Deployable {
            get {
                if (curved != null)
                    return curved.Deployable (InternalModule);
                return
                    InternalPanel.Events ["Extend"].active || InternalPanel.Events ["Retract"].active ||
                    InternalPanel.deployState == ModuleDeployablePart.DeployState.EXTENDING ||
                    InternalPanel.deployState == ModuleDeployablePart.DeployState.RETRACTING;
            }
        }

        /// <summary>
        /// Whether the solar panel is extended.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get {
                var state = DeployState;
                return
                    state == ModuleDeployablePart.DeployState.EXTENDED ||
                    state == ModuleDeployablePart.DeployState.EXTENDING;
            }
            set {
                if (!Deployable)
                    throw new InvalidOperationException ("Solar panel is not deployable");
                if (curved != null)
                    (value ? curved.Deploy : curved.Retract) (InternalModule);
                else if (value)
                    InternalPanel.Extend ();
                else
                    InternalPanel.Retract ();
            }
        }

        /// <summary>
        /// The current state of the solar panel.
        /// </summary>
        [KRPCProperty]
        public DeployableState State {
            get { return DeployState.ToDeployableState (); }
        }

        /// <summary>
        /// The current amount of energy being generated by the solar panel, in
        /// units of charge per second.
        /// </summary>
        [KRPCProperty]
        public float EnergyFlow {
            get {
                if (curved == null)
                    return InternalPanel.flowRate;
                return Generating ? curved.EnergyFlow (InternalModule) : 0;
            }
        }

        /// <summary>
        /// The current amount of sunlight that is incident on the solar panel,
        /// as a percentage. A value between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public float SunExposure {
            get {
                if (curved == null)
                    return InternalPanel.sunAOA;
                return Generating ? curved.SunExposure (InternalModule) : 0;
            }
        }
    }

    /// <summary>
    /// The members of the Near Future Solar curved panel module, ModuleCurvedSolarPanel,
    /// found by name for each module type.
    /// </summary>
    sealed class CurvedSolarPanel
    {
        const string ModuleName = "ModuleCurvedSolarPanel";

        static readonly IDictionary<Type, CurvedSolarPanel> types = new Dictionary<Type, CurvedSolarPanel> ();

        internal readonly Func<global::PartModule, bool> Deployable;
        internal readonly Func<global::PartModule, ModuleDeployablePart.DeployState> State;
        internal readonly Func<global::PartModule, float> EnergyFlow;
        internal readonly Func<global::PartModule, float> SunExposure;
        internal readonly Action<global::PartModule> Deploy;
        internal readonly Action<global::PartModule> Retract;

        CurvedSolarPanel (Type type)
        {
            Deployable = Accessor.Getter<global::PartModule, bool> (type, "Deployable");
            State = Accessor.Getter<global::PartModule, ModuleDeployablePart.DeployState> (type, "State");
            EnergyFlow = Accessor.Getter<global::PartModule, float> (type, "energyFlow");
            SunExposure = Accessor.Getter<global::PartModule, float> (type, "sunExposure");
            Deploy = Accessor.Method<Action<global::PartModule>> (type, "Deploy");
            Retract = Accessor.Method<Action<global::PartModule>> (type, "Retract");
        }

        bool Complete {
            get {
                return Deployable != null && State != null && EnergyFlow != null &&
                    SunExposure != null && Deploy != null && Retract != null;
            }
        }

        /// <summary>
        /// The part's curved panel module, or null if it has none, or its members are missing.
        /// </summary>
        internal static global::PartModule Find (global::Part part)
        {
            var module = part.Module (ModuleName);
            return module != null && For (module.GetType ()) != null ? module : null;
        }

        /// <summary>
        /// The accessors for the module type, or null if any of its members are missing.
        /// </summary>
        internal static CurvedSolarPanel For (Type type)
        {
            return types.GetOrAdd (type, Create);
        }

        static CurvedSolarPanel Create (Type type)
        {
            var panel = new CurvedSolarPanel (type);
            if (panel.Complete)
                return panel;
            Logger.WriteLine (
                "Near Future Solar " + ModuleName + " is missing members kRPC uses. Its parts are not solar panels.",
                Logger.Severity.Warning);
            return null;
        }
    }
}
