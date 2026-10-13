using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An action, belonging to a part module, that is assigned to an action group.
    /// Obtained by calling <see cref="Control.GetActionGroupActions"/>.
    /// </summary>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct ActionGroupAction
    {
        internal ActionGroupAction (Part actionPart, Module actionModule, string actionName, string actionId) : this ()
        {
            Part = actionPart;
            Module = actionModule;
            Name = actionName;
            Id = actionId;
        }

        /// <summary>
        /// The part that the action acts on.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; set; }

        /// <summary>
        /// The part module that the action belongs to. Returns <c>null</c> for a
        /// part-level action that is not associated with a module. This only occurs when
        /// the Extended Action Groups mod is installed, as it can assign actions defined
        /// directly on a part, rather than on one of its modules, to an action group.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Module Module { get; set; }

        /// <summary>
        /// The human-readable name of the action, as shown in the action group editor.
        /// </summary>
        [KRPCProperty]
        public string Name { get; set; }

        /// <summary>
        /// The non-localized identifier for the action.
        /// </summary>
        [KRPCProperty]
        public string Id { get; set; }
    }
}
