using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselControlStateExtensions
    {
        public static ControlSource ToControlSource (this CommNet.VesselControlState state)
        {
            // KSP sets the kerbal and probe bits from the kind of command module the vessel
            // carries, whether or not it is currently giving any control: a command pod with
            // no crew aboard reports KerbalNone. A vessel that is not being controlled has no
            // control source, so the level decides before the kind does.
            if (state.ToControlState () == ControlState.None)
                return ControlSource.None;
            if ((state & CommNet.VesselControlState.Kerbal) != 0)
                return ControlSource.Kerbal;
            if ((state & CommNet.VesselControlState.Probe) != 0)
                return ControlSource.Probe;
            return ControlSource.None;
        }

        public static ControlState ToControlState (this CommNet.VesselControlState state)
        {
            if ((state & CommNet.VesselControlState.Full) != 0)
                return ControlState.Full;
            if ((state & CommNet.VesselControlState.Partial) != 0)
                return ControlState.Partial;
            return ControlState.None;
        }
    }
}
