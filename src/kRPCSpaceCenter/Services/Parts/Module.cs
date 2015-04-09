using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class Module : Equatable<Module>
    {
        readonly Part part;
        readonly PartModule module;

        internal Module (Part part, PartModule module)
        {
            this.part = part;
            this.module = module;
        }

        public override bool Equals (Module obj)
        {
            return part == obj.part && module == obj.module;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode () ^ module.GetHashCode ();
        }

        [KRPCProperty]
        public string Name {
            get { return module.moduleName; }
        }

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        IEnumerable<BaseField> AllFields {
            get { return module.Fields.Cast<BaseField> ().Where (f => f != null && (HighLogic.LoadedSceneIsEditor ? f.guiActiveEditor : f.guiActive)); }
        }

        IEnumerable<BaseEvent> AllEvents {
            get { return module.Events.Where (e => e != null && (HighLogic.LoadedSceneIsEditor ? e.guiActiveEditor : e.guiActive) && e.active); }
        }

        IEnumerable<BaseAction> AllActions {
            get { return module.Actions; }
        }

        [KRPCProperty]
        public IDictionary<string,string> Fields {
            get {
                var result = new Dictionary<string,string> ();
                foreach (var field in AllFields)
                    result [field.guiName] = field.GetValue (module).ToString ();
                return result;
            }
        }

        [KRPCMethod]
        public bool HasField (string name)
        {
            return AllFields.Any (x => x.guiName == name);
        }

        [KRPCMethod]
        public string GetField (string name)
        {
            return AllFields.First (x => x.guiName == name).GetValue (module).ToString ();
        }

        [KRPCProperty]
        public IList<string> Events {
            get { return AllEvents.Select (x => x.guiName).ToList (); }
        }

        [KRPCMethod]
        public bool HasEvent (string name)
        {
            return AllEvents.Any (x => x.guiName == name);
        }

        [KRPCMethod]
        public void TriggerEvent (string name)
        {
            AllEvents.First (x => x.guiName == name).Invoke ();
        }

        [KRPCProperty]
        public IList<string> Actions {
            get { return AllActions.Select (a => a.guiName).ToList (); }
        }

        [KRPCMethod]
        public bool HasAction (string name)
        {
            return AllActions.Any (x => x.guiName == name);
        }

        [KRPCMethod]
        public void SetAction (string name, bool value = true)
        {
            var action = AllActions.First (a => a.guiName == name);
            action.Invoke (new KSPActionParam (action.actionGroup, (value ? KSPActionType.Activate : KSPActionType.Deactivate)));
        }
    }
}
