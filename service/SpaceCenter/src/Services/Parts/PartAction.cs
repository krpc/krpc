using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An action of a part module. These are the part actions that can be assigned to action groups
    /// in the in-game editor. Obtained by calling <see cref="Module.ActionList"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class PartAction : Equatable<PartAction>, IGameObjectState
    {
        readonly string name;

        internal PartAction (Module module, BaseAction baseAction)
        {
            Module = module;
            name = baseAction.name;
        }

        /// <summary>
        /// The underlying KSP action, found on the module by its identifier.
        /// </summary>
        BaseAction InternalAction {
            get {
                var action = Module.InternalModule.Actions [name];
                if (action == null)
                    throw new ObjectDestroyedException (
                        "The part module no longer has a action called " + name + ".");
                return action;
            }
        }

        /// <summary>
        /// The state of the module this belongs to.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Module.GameObjectState; }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PartAction other)
        {
            return !ReferenceEquals (other, null) && Module == other.Module && name == other.name;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Module).And (name);
        }

        /// <summary>
        /// The part module that contains this action.
        /// </summary>
        [KRPCProperty]
        public Module Module { get; private set; }

        /// <summary>
        /// The identifier of the action. This is stable and does not change between game versions,
        /// unlike <see cref="GuiName"/>.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return name; }
        }

        /// <summary>
        /// The name of the action, as displayed in the in-game editor.
        /// </summary>
        [KRPCProperty]
        public string GuiName {
            get { return InternalAction.guiName; }
        }

        /// <summary>
        /// Activate or deactivate the action. Equivalent to triggering the action from an action
        /// group. Set to <c>true</c> to activate the action, or <c>false</c> to deactivate it.
        /// </summary>
        [KRPCProperty]
        public bool Activated {
            set {
                InternalAction.Invoke (new KSPActionParam (
                    InternalAction.actionGroup,
                    value ? KSPActionType.Activate : KSPActionType.Deactivate
                ));
            }
        }
    }
}
