using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An action of a part module. These are the part actions that can be assigned to action groups
    /// in the in-game editor. Obtained by calling <see cref="Module.ActionList"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class PartAction : Equatable<PartAction>
    {
        readonly BaseAction action;

        internal PartAction (Module module, BaseAction baseAction)
        {
            Module = module;
            action = baseAction;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PartAction other)
        {
            return !ReferenceEquals (other, null) && Module == other.Module && ReferenceEquals (action, other.action);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Module.GetHashCode () ^ action.GetHashCode ();
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
            get { return action.name; }
        }

        /// <summary>
        /// The name of the action, as displayed in the in-game editor.
        /// </summary>
        [KRPCProperty]
        public string GuiName {
            get { return action.guiName; }
        }

        /// <summary>
        /// Activate or deactivate the action. Equivalent to triggering the action from an action
        /// group.
        /// </summary>
        /// <param name="value"><c>true</c> to activate the action, <c>false</c> to deactivate it.</param>
        [KRPCMethod]
        public void Set (bool value = true)
        {
            action.Invoke (new KSPActionParam (
                action.actionGroup,
                value ? KSPActionType.Activate : KSPActionType.Deactivate
            ));
        }
    }
}
