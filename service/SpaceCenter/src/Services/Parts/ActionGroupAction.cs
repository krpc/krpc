using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An action, belonging to a part module, that is assigned to an action group.
    /// Obtained by calling <see cref="Control.GetActionGroupActions"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ActionGroupAction : Equatable<ActionGroupAction>
    {
        readonly Part part;
        readonly Module module;
        readonly string name;
        readonly string id;

        internal ActionGroupAction (Part actionPart, Module actionModule, string actionName, string actionId)
        {
            part = actionPart;
            module = actionModule;
            name = actionName;
            id = actionId;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ActionGroupAction other)
        {
            return !ReferenceEquals (other, null) &&
                part == other.part &&
                module == other.module &&
                name == other.name &&
                id == other.id;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            // module can be null for a part-level action (Part.Actions) that the Extended
            // Action Groups mod has assigned to a group, so it is not included in the hash.
            int hash = part.GetHashCode () ^ name.GetHashCode () ^ id.GetHashCode ();
            if (module != null)
                hash ^= module.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part that the action acts on.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// The part module that the action belongs to.
        /// </summary>
        [KRPCProperty]
        public Module Module {
            get { return module; }
        }

        /// <summary>
        /// The human-readable name of the action, as shown in the action group editor.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return name; }
        }

        /// <summary>
        /// The non-localized identifier for the action.
        /// </summary>
        [KRPCProperty]
        public string Id {
            get { return id; }
        }
    }
}
