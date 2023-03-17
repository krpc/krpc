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

        IEnumerable<BaseField> AllFields {
            get { return module.Fields.Cast<BaseField> ().Where (f => f != null && (HighLogic.LoadedSceneIsEditor ? f.guiActiveEditor : f.guiActive)); }
        }

        IEnumerable<BaseEvent> AllEvents {
            get { return module.Events.Where (e => e != null && (HighLogic.LoadedSceneIsEditor ? e.guiActiveEditor : e.guiActive) && e.active); }
        }

        IEnumerable<BaseAction> AllActions {
            get { return module.Actions; }
        }

        /// <summary>
        /// The modules field names and their associated values, as a dictionary.
        /// These are the values visible in the right-click menu of the part.
        /// </summary>
        /// <remarks>
        /// Throws an exception if there is more than one field with the same name.
        /// In that case, use <see cref="FieldsById"/> to get the fields by identifier.
        /// </remarks>
        [KRPCProperty]
        public IDictionary<string,string> Fields {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var field in AllFields)
                    result.Add (field.guiName, field.GetValue (module).ToString ());
                return result;
            }
        }

        /// <summary>
        /// The modules field identifiers and their associated values, as a dictionary.
        /// These are the values visible in the right-click menu of the part.
        /// </summary>
        [KRPCProperty]
        public IDictionary<string,string> FieldsById {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var field in AllFields)
                    result.Add (field.name, field.GetValue (module).ToString ());
                return result;
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the module has a field with the given name.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        [KRPCMethod]
        public bool HasField (string name)
        {
            return AllFields.Any (x => x.guiName == name);
        }

        /// <summary>
        /// Returns <c>true</c> if the module has a field with the given identifier.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        [KRPCMethod]
        public bool HasFieldWithId (string id)
        {
            return AllFields.Any (x => x.name == id);
        }

        BaseField GetBaseFieldByName (string name)
        {
            return AllFields.First (x => x.guiName == name);
        }

        BaseField GetBaseFieldById (string id)
        {
            return AllFields.First (x => x.name == id);
        }

        /// <summary>
        /// Returns the value of a field with the given name.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        [KRPCMethod]
        public string GetField (string name)
        {
            return GetBaseFieldByName (name).GetValue (module).ToString ();
        }

        /// <summary>
        /// Returns the value of a field with the given identifier.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
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
        [KRPCMethod]
        public void SetFieldBoolById (string id, bool value)
        {
            AssignFieldById(id, value);
        }

        /// <summary>
        /// Set the value of a field to its original value.
        /// </summary>
        /// <param name="name">Name of the field.</param>
        [KRPCMethod]
        public void ResetField (string name)
        {
            GetBaseFieldByName (name).SetOriginalValue ();
        }

        /// <summary>
        /// Set the value of a field to its original value.
        /// </summary>
        /// <param name="id">Identifier of the field.</param>
        [KRPCMethod]
        public void ResetFieldById (string id)
        {
            GetBaseFieldById (id).SetOriginalValue ();
        }

        /// <summary>
        /// A list of the names of all of the modules events. Events are the clickable buttons
        /// visible in the right-click menu of the part.
        /// </summary>
        [KRPCProperty]
        public IList<string> Events {
            get { return AllEvents.Select (x => x.guiName).ToList (); }
        }

        /// <summary>
        /// A list of the identifiers of all of the modules events. Events are the clickable buttons
        /// visible in the right-click menu of the part.
        /// </summary>
        [KRPCProperty]
        public IList<string> EventsById {
            get { return AllEvents.Select (x => x.name).ToList (); }
        }

        /// <summary>
        /// <c>true</c> if the module has an event with the given name.
        /// </summary>
        /// <param name="name"></param>
        [KRPCMethod]
        public bool HasEvent (string name)
        {
            return AllEvents.Any (x => x.guiName == name);
        }

        /// <summary>
        /// <c>true</c> if the module has an event with the given identifier.
        /// </summary>
        /// <param name="id"></param>
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
        [KRPCMethod]
        public void TriggerEvent (string name)
        {
            AllEvents.First (x => x.guiName == name).Invoke ();
        }

        /// <summary>
        /// Trigger the event with the given identifier.
        /// Equivalent to clicking the button in the right-click menu of the part.
        /// </summary>
        /// <param name="id"></param>
        [KRPCMethod]
        public void TriggerEventById (string id)
        {
            AllEvents.First (x => x.name == id).Invoke ();
        }

        /// <summary>
        /// A list of all the names of the modules actions. These are the parts actions that can
        /// be assigned to action groups in the in-game editor.
        /// </summary>
        [KRPCProperty]
        public IList<string> Actions {
            get { return AllActions.Select (a => a.guiName).ToList (); }
        }

        /// <summary>
        /// A list of all the identifiers of the modules actions. These are the parts actions
        /// that can be assigned to action groups in the in-game editor.
        /// </summary>
        [KRPCProperty]
        public IList<string> ActionsById {
            get { return AllActions.Select (a => a.name).ToList (); }
        }

        /// <summary>
        /// <c>true</c> if the part has an action with the given name.
        /// </summary>
        /// <param name="name"></param>
        [KRPCMethod]
        public bool HasAction (string name)
        {
            return AllActions.Any (x => x.guiName == name);
        }

        /// <summary>
        /// <c>true</c> if the part has an action with the given identifier.
        /// </summary>
        /// <param name="id"></param>
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
        [KRPCMethod]
        public void SetActionById (string id, bool value = true)
        {
            SetAction(AllActions.First (a => a.name == id), value);
        }
    }
}
