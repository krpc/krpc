using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPCInfernalRobotics.Services
{
    [KRPCClass (Service = "InfernalRobotics")]
    public sealed class Servo : Equatable<Servo>
    {
        readonly IRWrapper.IServo servo;

        internal Servo (IRWrapper.IServo servo)
        {
            this.servo = servo;
        }

        public override bool Equals (Servo obj)
        {
            return servo == obj.servo;
        }

        public override int GetHashCode ()
        {
            return servo.GetHashCode ();
        }

        [KRPCProperty]
        public string Name { get { return servo.Name; } }

        [KRPCProperty]
        public bool Highlight { set { servo.Highlight = value; } }

        [KRPCProperty]
        public float Position { get { return servo.Position; } }

        [KRPCProperty]
        public float MinConfigPosition { get { return servo.MinConfigPosition; } }

        [KRPCProperty]
        public float MaxConfigPosition { get { return servo.MaxConfigPosition; } }

        [KRPCProperty]
        public float MinPosition { get { return servo.MinPosition; } }

        [KRPCProperty]
        public float MaxPosition { get { return servo.MaxPosition; } }

        [KRPCProperty]
        public float ConfigSpeed { get { return servo.ConfigSpeed; } }

        [KRPCProperty]
        public float Speed { get { return servo.Speed; } }

        [KRPCProperty]
        public float CurrentSpeed { get { return servo.CurrentSpeed; } }

        [KRPCProperty]
        public float Acceleration { get { return servo.Acceleration; } }

        [KRPCProperty]
        public bool IsMoving { get { return servo.IsMoving; } }

        [KRPCProperty]
        public bool IsFreeMoving { get { return servo.IsFreeMoving; } }

        [KRPCProperty]
        public bool IsLocked { get { return servo.IsLocked; } }

        [KRPCProperty]
        public bool IsAxisInverted { get { return servo.IsAxisInverted; } }

        [KRPCMethod]
        public void MoveRight ()
        {
            servo.MoveRight ();
        }

        [KRPCMethod]
        public void MoveLeft ()
        {
            servo.MoveLeft ();
        }

        [KRPCMethod]
        public void MoveCenter ()
        {
            servo.MoveCenter ();
        }

        [KRPCMethod]
        public void MoveNextPreset ()
        {
            servo.MoveNextPreset ();
        }

        [KRPCMethod]
        public void MovePrevPreset ()
        {
            servo.MovePrevPreset ();
        }

        [KRPCMethod]
        public void MoveTo (float position, float speed)
        {
            servo.MoveTo (position, speed);
        }

        [KRPCMethod]
        public void Stop ()
        {
            servo.Stop ();
        }
    }
}
