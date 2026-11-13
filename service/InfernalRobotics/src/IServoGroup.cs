using System.Collections.Generic;

namespace KRPC.InfernalRobotics
{
    /// <summary>
    /// A servo group, held by IR's controller or built by kRPC from a vessel's servo modules.
    /// </summary>
    interface IServoGroup
    {
        string Name { get; set; }
        global::Vessel Vessel { get; }
        float GroupSpeedFactor { get; set; }
        bool Expanded { get; set; }
        int MovingDirection { get; }
        bool AdvancedMode { get; set; }
        float TotalElectricChargeRequirement { get; }
        bool BuildAid { get; set; }
        bool IKActive { get; set; }
        string ForwardKey { get; set; }
        string ReverseKey { get; set; }
        IList<IRServo> Servos { get; }

        void MoveLeft ();
        void MoveCenter ();
        void MoveRight ();
        void MoveNextPreset ();
        void MovePrevPreset ();
        void Stop ();
    }
}
