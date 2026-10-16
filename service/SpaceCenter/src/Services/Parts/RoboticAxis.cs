using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An axis of a robotic controller.
    /// </summary>
    /// <remarks>
    /// See <see cref="RoboticController.Axes"/>.
    /// </remarks>
    [KRPCStruct (Service = "SpaceCenter")]
    public struct RoboticAxis
    {
        internal RoboticAxis (Expansions.Serenity.ControlledAxis axis) : this ()
        {
            Module = new Module (new Part (axis.Part), axis.Module);
            FieldName = axis.AxisField.name;
        }

        /// <summary>
        /// The part module the axis belongs to.
        /// </summary>
        [KRPCProperty]
        public Module Module { get; set; }

        /// <summary>
        /// The name of the field the axis controls.
        /// </summary>
        [KRPCProperty]
        public string FieldName { get; set; }
    }
}
