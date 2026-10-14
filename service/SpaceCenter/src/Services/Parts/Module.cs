using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// This can be used to interact with a specific part module. This includes part modules in
    /// stock KSP, and those added by mods.
    ///
    /// In KSP, each part has zero or more
    /// <a href="https://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation#MODULES">PartModules</a>
    /// associated with it. Each one contains some of the functionality of the part.
    /// For example, an engine has a "ModuleEngines" part module that contains all the
    /// functionality of an engine.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Module : Equatable<Module>, IGameObjectState
    {
        ModuleRef reference;

        internal Module (Part part, PartModule partModule)
        {
            Part = part;
            reference = ModuleRef.ForModule (partModule);
        }

        /// <summary>
        /// The underlying KSP part module. Used by <see cref="PartField"/>, <see cref="PartEvent"/>
        /// and <see cref="PartAction"/> to read and write against the host module.
        /// </summary>
        internal PartModule InternalModule {
            get { return reference.Get (Part.InternalPart); }
        }

        /// <summary>
        /// The state of the module. A module belongs to its part's game object, and takes
        /// the part's state.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return reference.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Module other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && reference == other.reference;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Part).And (reference);
        }

        /// <summary>
        /// Name of the PartModule. For example, "ModuleEngines".
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return InternalModule.moduleName; }
        }

        /// <summary>
        /// The part that contains this module.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The static configuration of the module, as found in the part's
        /// <a href="https://wiki.kerbalspaceprogram.com/wiki/CFG_File_Documentation#MODULES">cfg file</a>.
        /// This provides access to data that is not exposed as a field, such as the
        /// resources produced by a generator. Returns <c>null</c> if the module's
        /// configuration node cannot be found.
        /// </summary>
        [KRPCProperty]
        public ConfigNode Config {
            get {
                var node = FindConfigNode ();
                return node == null ? null : new ConfigNode (node);
            }
        }

        global::ConfigNode FindConfigNode ()
        {
            var part = Part.InternalPart;
            var info = part.partInfo;
            if (info == null || info.partConfig == null)
                return null;
            // Find the position of this module among the modules on the part that share
            // its name, then return the config node for the same occurrence.
            var module = reference.Get (part);
            int occurrence = 0;
            var modules = part.Modules;
            for (int i = 0; i < modules.Count; i++) {
                if (ReferenceEquals (modules [i], module))
                    break;
                if (modules [i].moduleName == module.moduleName)
                    occurrence++;
            }
            int seen = 0;
            foreach (var moduleNode in info.partConfig.GetNodes ("MODULE")) {
                if (moduleNode.GetValue ("name") == module.moduleName) {
                    if (seen == occurrence)
                        return moduleNode;
                    seen++;
                }
            }
            return null;
        }

        IEnumerable<BaseField> AllBaseFields {
            get { return InternalModule.Fields.Cast<BaseField> ().Where (f => f != null); }
        }

        IEnumerable<BaseEvent> AllEvents {
            get { return InternalModule.Events.Where (e => e != null && (HighLogic.LoadedSceneIsEditor ? e.guiActiveEditor : e.guiActive) && e.active); }
        }

        IEnumerable<BaseEvent> AllBaseEvents {
            get { return InternalModule.Events.Where (e => e != null); }
        }

        IEnumerable<BaseAction> AllActions {
            get { return InternalModule.Actions; }
        }

        // The following three helpers match on the display name, which the game translates.
        // To identify a known event from within kRPC, use the ById helpers below

        internal IEnumerable<string> VisibleEventNames {
            get { return AllEvents.Select (x => x.guiName); }
        }

        internal bool HasVisibleEvent (string name)
        {
            return AllEvents.Any (x => x.guiName == name);
        }

        internal void TriggerVisibleEvent (string name)
        {
            AllEvents.First (x => x.guiName == name).Invoke ();
        }

        // Events are identified by their id, the name of the method implementing them, which
        // the game does not translate, unlike the display name

        internal bool HasVisibleEventById (string id)
        {
            return AllEvents.Any (x => x.name == id);
        }

        internal void TriggerVisibleEventById (string id)
        {
            AllEvents.First (x => x.name == id).Invoke ();
        }

        /// <summary>
        /// A list of all the fields of the module, including those not visible in the right-click
        /// menu of the part. Filter by <see cref="PartField.Visible"/> to get just the visible ones.
        /// </summary>
        [KRPCProperty]
        public IList<PartField> FieldList {
            get { return AllBaseFields.Select (f => new PartField (this, f)).ToList (); }
        }

        /// <summary>
        /// A list of all the events of the module, including those not currently visible or active.
        /// Events are the clickable buttons visible in the right-click menu of the part. Filter by
        /// <see cref="PartEvent.Visible"/> and <see cref="PartEvent.Active"/> to get just the ones
        /// shown in the menu.
        /// </summary>
        [KRPCProperty]
        public IList<PartEvent> EventList {
            get { return AllBaseEvents.Select (e => new PartEvent (this, e)).ToList (); }
        }

        /// <summary>
        /// A list of all the actions of the module. These are the parts actions that can be assigned
        /// to action groups in the in-game editor.
        /// </summary>
        [KRPCProperty]
        public IList<PartAction> ActionList {
            get { return AllActions.Select (a => new PartAction (this, a)).ToList (); }
        }

        /// <summary>
        /// Restore a field to its original value: the value KSP recorded when the part was
        /// loaded. Note that BaseField.SetOriginalValue does not do this -- despite its name,
        /// it re-snapshots the current value as the new original -- so instead this writes
        /// originalValue back through BaseField.SetValue (which also fires OnValueModified).
        /// </summary>
        internal void RestoreOriginalFieldValue(BaseField<KSPField> field) {
            var original = field.originalValue;
            if (original == null && field.FieldInfo.FieldType.IsValueType)
                return;
            field.SetValue (original, InternalModule);
        }
    }
}
