using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class VesselControlStateExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static ControlSource ToControlSource (this CommNet.VesselControlState state)
        {
            if ((state & CommNet.VesselControlState.Kerbal) != 0)
                return ControlSource.Kerbal;
            if ((state & CommNet.VesselControlState.Probe) != 0)
                return ControlSource.Probe;
            return ControlSource.None;
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
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
