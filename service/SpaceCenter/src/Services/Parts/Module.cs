using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
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
    public class Module : Equatable<Module>
    {
        readonly PartModule module;

        internal Module (Part part, PartModule partModule)
        {
            Part = part;
            module = partModule;
        }

        /// <summary>
        /// The underlying KSP part module. Used by <see cref="PartField"/>, <see cref="PartEvent"/>
        /// and <see cref="PartAction"/> to read and write against the host module.
        /// </summary>
        internal PartModule InternalModule {
            get { return module; }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Module other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && module.Equals (other.module);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ module.GetHashCode ();
        }

        /// <summary>
        /// Name of the PartModule. For example, "ModuleEngines".
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return module.moduleName; }
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
            var info = Part.InternalPart.partInfo;
            if (info == null || info.partConfig == null)
                return null;
            // Find the position of this module among the modules on the part that share
            // its name, then return the config node for the same occurrence.
            int occurrence = 0;
            var modules = Part.InternalPart.Modules;
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

        IEnumerable<BaseField> VisibleFields {
            get { return module.Fields.Cast<BaseField> ().Where (f => f != null && (HighLogic.LoadedSceneIsEditor ? f.guiActiveEditor : f.guiActive)); }
        }

        IEnumerable<BaseField> AllBaseFields {
            get { return module.Fields.Cast<BaseField> ().Where (f => f != null); }
        }

        IEnumerable<BaseEvent> AllEvents {
            get { return module.Events.Where (e => e != null && (HighLogic.LoadedSceneIsEditor ? e.guiActiveEditor : e.guiActive) && e.active); }
        }

        IEnumerable<BaseEvent> AllBaseEvents {
            get { return module.Events.Where (e => e != null); }
        }

        IEnumerable<BaseAction> AllActions {
            get { return module.Actions; }
        }

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
        /// The modules field names and their associated values, as a dictionary.
        /// These are the values visible in the right-click menu of the part.
        /// </summary>
        /// <remarks>
        /// Throws an exception if there is more than one field with the same name.
        /// In that case, use <see cref="FieldsById"/> to get the fields by identifier.
        /// </remarks>
        [Obsolete("Use <see cref='FieldList'/> instead, filtering by <see cref='PartField.Visible'/>.")]
        [KRPCProperty]
        public IDictionary<string,string> Fields {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var f in VisibleFields)
                    result.Add (f.guiName, f.GetValue (module).ToString ());
                return result;
            }
        }

        /// <summary>
        /// The modules field identifiers and their associated values, as a dictionary.
        /// These are the values visible in the right-click menu of the part.
        /// </summary>
        [Obsolete("Use <see cref='FieldList'/> instead, filtering by <see cref='PartField.Visible'/>.")]
        [KRPCProperty]
        public IDictionary<string,string> FieldsById {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var f in VisibleFields)
                    result.Add (f.name, f.GetValue (module).ToString ());
                return result;
            }
        }

        /// <summary>
        /// The modules field identifiers and their associated values, as a dictionary.
        /// This is the same as <see cref="FieldsById"/>, except that it also includes
        /// fields that are not visible in the right-click menu of the part.
        /// </summary>
        /// <remarks>
        /// Throws an exception if there is more than one field with the same identifier.
        /// </remarks>
        [Obsolete("Use <see cref='FieldList'/> instead.")]
        [KRPCProperty]
        public IDictionary<string,string> AllFieldsById {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var f in AllBaseFields)
                    result.Add (f.name, f.GetValue (module).ToString ());
                return result;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the module has a field with the given name.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        [Obsolete("Filter <see cref='FieldList'/> by <see cref='PartField.GuiName'/> instead.")]
        [KRPCMethod]
        public bool HasField (string name)
        {
            return VisibleFields.Any (x => x.guiName == name);
        }

        /// <summary>
        /// Returns <c>true</c> if the module has a field with the given identifier.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        [Obsolete("Filter <see cref='FieldList'/> by <see cref='PartField.Name'/> instead.")]
        [KRPCMethod]
        public bool HasFieldWithId (string id)
        {
            return AllBaseFields.Any (x => x.name == id);
        }

        BaseField GetBaseFieldByName (string name)
        {
            return VisibleFields.First (x => x.guiName == name);
        }

        BaseField GetBaseFieldById (string id)
        {
            return VisibleFields.FirstOrDefault (x => x.name == id) ?? AllBaseFields.First (x => x.name == id);
        }

        /// <summary>
        /// Returns the value of a field with the given name.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        [Obsolete("Filter <see cref='FieldList'/> by <see cref='PartField.GuiName'/> and read <see cref='PartField.Value'/> instead.")]
        [KRPCMethod]
        public string GetField (string name)
        {
            return GetBaseFieldByName (name).GetValue (module).ToString ();
        }

        /// <summary>
        /// Returns the value of a field with the given identifier.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        [Obsolete("Filter <see cref='FieldList'/> by <see cref='PartField.Name'/> and read <see cref='PartField.Value'/> instead.")]
        [KRPCMethod]
        public string GetFieldById (string id)
        {
            return GetBaseFieldById (id).GetValue (module).ToString ();
        }

        /// <summary>
        /// BaseField.SetValue fails silently if the value being set is of the wrong type.
        /// (It only outputs a message to the debug log, it does not throw on error.)
        /// This method first checks that the type of the field is assignable from the type
        /// of the value being set, and throws an exception if not.
        /// </summary>
        private void AssignField(BaseField<KSPField> field, object value) {
            var type = field.FieldInfo.FieldType;
            if (!type.IsAssignableFrom(value.GetType()))
                throw new ArgumentException(
                    "Cannot set field with type " + type + " to a value of type " + value.GetType());
            field.SetValue (value, module);
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
            field.SetValue (original, module);
        }

        private void AssignFieldByName(string name, object value) {
            AssignField(GetBaseFieldByName (name), value);
        }

        private void AssignFieldById(string id, object value) {
            AssignField(GetBaseFieldById (id), value);
        }

        /// <summary>
        /// Set the value of a field to the given integer number.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.IntValue'/> instead.")]
        [KRPCMethod]
        public void SetFieldInt (string name, int value)
        {
            AssignFieldByName(name, value);
        }

        /// <summary>
        /// Set the value of a field to the given integer number.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.IntValue'/> instead.")]
        [KRPCMethod]
        public void SetFieldIntById (string id, int value)
        {
            AssignFieldById(id, value);
        }

        /// <summary>
        /// Set the value of a field to the given floating point number.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.FloatValue'/> instead.")]
        [KRPCMethod]
        public void SetFieldFloat (string name, float value)
        {
            AssignFieldByName(name, value);
        }

        /// <summary>
        /// Set the value of a field to the given floating point number.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.FloatValue'/> instead.")]
        [KRPCMethod]
        public void SetFieldFloatById (string id, float value)
        {
            AssignFieldById(id, value);
        }

        /// <summary>
        /// Set the value of a field to the given string.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.Value'/> instead.")]
        [KRPCMethod]
        public void SetFieldString (string name, string value)
        {
            AssignFieldByName(name, value);
        }

        /// <summary>
        /// Set the value of a field to the given string.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.Value'/> instead.")]
        [KRPCMethod]
        public void SetFieldStringById (string id, string value)
        {
            AssignFieldById(id, value);
        }

        /// <summary>
        /// Set the value of a field to true or false.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.BoolValue'/> instead.")]
        [KRPCMethod]
        public void SetFieldBool (string name, bool value)
        {
            AssignFieldByName(name, value);
        }

        /// <summary>
        /// Set the value of a field to true or false.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        /// <param name="value">Value to set.</param>
        [Obsolete("Set <see cref='PartField.BoolValue'/> instead.")]
        [KRPCMethod]
        public void SetFieldBoolById (string id, bool value)
        {
            AssignFieldById(id, value);
        }

        /// <summary>
        /// Set the value of a field to its original value.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        [Obsolete("Use <see cref='PartField.Reset'/> instead.")]
        [KRPCMethod]
        public void ResetField (string name)
        {
            RestoreOriginalFieldValue (GetBaseFieldByName (name));
        }

        /// <summary>
        /// Set the value of a field to its original value.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        /// <remarks>
        /// The original value is the value the field had when the part was loaded.
        /// </remarks>
        [Obsolete("Use <see cref='PartField.Reset'/> instead.")]
        [KRPCMethod]
        public void ResetFieldById (string id)
        {
            RestoreOriginalFieldValue (GetBaseFieldById (id));
        }

        /// <summary>
        /// A list of the names of all of the modules events. Events are the clickable buttons
        /// visible in the right-click menu of the part.
        /// </summary>
        [Obsolete("Use <see cref='EventList'/> instead, filtering by <see cref='PartEvent.Visible'/> and <see cref='PartEvent.Active'/>.")]
        [KRPCProperty]
        public IList<string> Events {
            get { return VisibleEventNames.ToList (); }
        }

        /// <summary>
        /// A list of the identifiers of all of the modules events. Events are the clickable buttons
        /// visible in the right-click menu of the part.
        /// </summary>
        [Obsolete("Use <see cref='EventList'/> instead, filtering by <see cref='PartEvent.Visible'/> and <see cref='PartEvent.Active'/>.")]
        [KRPCProperty]
        public IList<string> EventsById {
            get { return AllEvents.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// <c>true</c> if the module has an event with the given name.
        /// </summary>
        /// <param name="name"></param>
        [Obsolete("Filter <see cref='EventList'/> by <see cref='PartEvent.GuiName'/> instead.")]
        [KRPCMethod]
        public bool HasEvent (string name)
        {
            return HasVisibleEvent (name);
        }

        /// <summary>
        /// <c>true</c> if the module has an event with the given identifier.
        /// </summary>
        /// <param name="id"></param>
        [Obsolete("Filter <see cref='EventList'/> by <see cref='PartEvent.Name'/> instead.")]
        [KRPCMethod]
        public bool HasEventWithId (string id)
        {
            return AllEvents.Any (x => x.name == id);
        }

        /// <summary>
        /// Trigger the named event. Equivalent to clicking the button in the right-click menu
        /// of the part.
        /// </summary>
        /// <param name="name"></param>
        [Obsolete("Filter <see cref='EventList'/> by <see cref='PartEvent.GuiName'/> and call <see cref='PartEvent.Trigger'/> instead.")]
        [KRPCMethod]
        public void TriggerEvent (string name)
        {
            TriggerVisibleEvent (name);
        }

        /// <summary>
        /// Trigger the event with the given identifier.
        /// Equivalent to clicking the button in the right-click menu of the part.
        /// </summary>
        /// <param name="id"></param>
        [Obsolete("Filter <see cref='EventList'/> by <see cref='PartEvent.Name'/> and call <see cref='PartEvent.Trigger'/> instead.")]
        [KRPCMethod]
        public void TriggerEventById (string id)
        {
            AllEvents.First (x => x.name == id).Invoke ();
        }

        /// <summary>
        /// A list of all the names of the modules actions. These are the parts actions that can
        /// be assigned to action groups in the in-game editor.
        /// </summary>
        [Obsolete("Use <see cref='ActionList'/> instead.")]
        [KRPCProperty]
        public IList<string> Actions {
            get { return AllActions.Select (a => a.guiName).ToList (); }
        }

        /// <summary>
        /// A list of all the identifiers of the modules actions. These are the parts actions
        /// that can be assigned to action groups in the in-game editor.
        /// </summary>
        [Obsolete("Use <see cref='ActionList'/> instead.")]
        [KRPCProperty]
        public IList<string> ActionsById {
            get { return AllActions.Select (a => a.name).ToList (); }
        }

        /// <summary>
        /// <c>true</c> if the part has an action with the given name.
        /// </summary>
        /// <param name="name"></param>
        [Obsolete("Filter <see cref='ActionList'/> by <see cref='PartAction.GuiName'/> instead.")]
        [KRPCMethod]
        public bool HasAction (string name)
        {
            return AllActions.Any (x => x.guiName == name);
        }

        /// <summary>
        /// <c>true</c> if the part has an action with the given identifier.
        /// </summary>
        /// <param name="id"></param>
        [Obsolete("Filter <see cref='ActionList'/> by <see cref='PartAction.Name'/> instead.")]
        [KRPCMethod]
        public bool HasActionWithId (string id)
        {
            return AllActions.Any (x => x.name == id);
        }

        private static void SetAction(BaseAction action, bool value)
        {
            action.Invoke (new KSPActionParam (
                action.actionGroup,
                (value ? KSPActionType.Activate : KSPActionType.Deactivate)
            ));
        }

        /// <summary>
        /// Set the value of an action with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        [Obsolete("Filter <see cref='ActionList'/> by <see cref='PartAction.GuiName'/> and set <see cref='PartAction.Activated'/> instead.")]
        [KRPCMethod]
        public void SetAction (string name, bool value = true)
        {
            SetAction(AllActions.First (a => a.guiName == name), value);
        }

        /// <summary>
        /// Set the value of an action with the given identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        [Obsolete("Filter <see cref='ActionList'/> by <see cref='PartAction.Name'/> and set <see cref='PartAction.Activated'/> instead.")]
        [KRPCMethod]
        public void SetActionById (string id, bool value = true)
        {
            SetAction(AllActions.First (a => a.name == id), value);
        }
    }
}
