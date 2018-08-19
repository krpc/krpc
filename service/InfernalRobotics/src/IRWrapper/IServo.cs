#pragma warning disable 1591

using System;

namespace KRPC.InfernalRobotics.IRWrapper
{
    public interface IServo : IEquatable<IServo>
    {
        string Name { get; set; }
        uint UID { get; }
        Part HostPart { get; }
        bool Highlight { set; }
        float Position { get; }
        float MinConfigPosition { get; }
        float MaxConfigPosition { get; }
        float MinPosition { get; set; }
        float MaxPosition { get; set; }
        float ConfigSpeed { get; }
        float Speed { get; set; }
        float CurrentSpeed { get; set; }
        float Acceleration { get; set; }
        bool IsMoving { get; }
        bool IsFreeMoving { get; }
        bool IsLocked { get; set; }
        bool IsAxisInverted { get; set; }
        void MoveRight ();
        void MoveLeft ();
        void MoveCenter ();
        void MoveNextPreset ();
        void MovePrevPreset ();
        void MoveTo (float position, float speed);
        void Stop ();
        bool Equals (object o);
        int GetHashCode ();
    }
}
