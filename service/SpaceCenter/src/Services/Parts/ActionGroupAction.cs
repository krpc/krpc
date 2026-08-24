using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An action, belonging to a part module, that is assigned to an action group.
    /// Obtained by calling <see cref="Control.GetActionGroupActions"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ActionGroupAction : Equatable<ActionGroupAction>, IGameObjectState
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
        /// What the game holds for the module the action belongs to, or for its part
        /// where the action has no module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return module != null ? module.GameObjectState : part.GameObjectState; }
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
            // module is null for a part-level action (Part.Actions) that the Extended
            // Action Groups mod has assigned to a group, and counts as zero.
            return Hash.Of (part).And (name).And (id).And (module);
        }

        /// <summary>
        /// The part that the action acts on.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// The part module that the action belongs to. Returns <c>null</c> for a
        /// part-level action that is not associated with a module. This only occurs when
        /// the Extended Action Groups mod is installed, as it can assign actions defined
        /// directly on a part, rather than on one of its modules, to an action group.
        /// </summary>
        [KRPCProperty (Nullable = true)]
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
