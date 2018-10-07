#pragma warning disable 1591

using System.Collections.Generic;

namespace KRPC.InfernalRobotics.IRWrapper
{
    public interface IRAPI
    {
        bool Ready { get; }
        IList<IControlGroup> ServoGroups { get; }
    }
}
