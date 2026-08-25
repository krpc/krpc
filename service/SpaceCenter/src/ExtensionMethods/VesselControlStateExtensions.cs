using System.Linq;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselControlStateExtensions
    {
        /// <summary>
        /// The control state a vessel is in, from the control level KSP gates its control on.
        ///
        /// This is deliberately taken from the vessel rather than from its communication
        /// network node: KSP consults the network only when CommNet is enabled, and works
        /// from the vessel's own parts otherwise, so the network's view of a vessel is not
        /// the whole answer.
        /// </summary>
        public static ControlState ToControlState (this global::Vessel.ControlLevel level)
        {
            switch (level) {
            case global::Vessel.ControlLevel.FULL:
                return ControlState.Full;
            case global::Vessel.ControlLevel.PARTIAL_MANNED:
            case global::Vessel.ControlLevel.PARTIAL_UNMANNED:
                return ControlState.Partial;
            default:
                return ControlState.None;
            }
        }

        /// <summary>
        /// What is controlling a vessel, from its control level and, where the level does not
        /// say, its parts. KSP records whether partial control comes from a kerbal or a probe,
        /// but not for full control, so that case is decided by whether any part that is a
        /// control source has crew aboard. A kerbal takes precedence over a probe core, as it
        /// does in KSP's own control state.
        /// </summary>
        public static ControlSource ToControlSource (
            this global::Vessel.ControlLevel level, global::Vessel vessel)
        {
            switch (level) {
            case global::Vessel.ControlLevel.FULL:
                return vessel.parts.Any (
                    part => part.isControlSource > global::Vessel.ControlLevel.NONE &&
                    part.protoModuleCrew.Count > 0) ? ControlSource.Kerbal : ControlSource.Probe;
            case global::Vessel.ControlLevel.PARTIAL_MANNED:
                return ControlSource.Kerbal;
            case global::Vessel.ControlLevel.PARTIAL_UNMANNED:
                return ControlSource.Probe;
            default:
                return ControlSource.None;
            }
        }
    }
}
