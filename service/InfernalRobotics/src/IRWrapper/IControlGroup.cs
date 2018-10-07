#pragma warning disable 1591

using System;
using System.Collections.Generic;

namespace KRPC.InfernalRobotics.IRWrapper
{
    public interface IControlGroup : IEquatable<IControlGroup>
    {
        string Name { get; set; }
        //can only be used in Flight, null checking is mandatory
        Vessel Vessel { get; }
        string ForwardKey { get; set; }
        string ReverseKey { get; set; }
        float Speed { get; set; }
        bool Expanded { get; set; }
        IList<IServo> Servos { get; }
        void MoveRight ();
        void MoveLeft ();
        void MoveCenter ();
        void MoveNextPreset ();
        void MovePrevPreset ();
        void Stop ();
    }
}
