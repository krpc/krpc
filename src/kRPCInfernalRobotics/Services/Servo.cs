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
        public string Name {
            get { return servo.Name; }
            set { servo.Name = value; }
        }

        [KRPCProperty]
        public bool Highlight { set { servo.Highlight = value; } }

        [KRPCProperty]
        public float Position {
            get { return servo.Position; }
        }

        [KRPCProperty]
        public float MinConfigPosition {
            get { return servo.MinConfigPosition; }
        }

        [KRPCProperty]
        public float MaxConfigPosition {
            get { return servo.MaxConfigPosition; }
        }

        [KRPCProperty]
        public float MinPosition {
            get { return servo.MinPosition; }
            set { servo.MinPosition = value; }
        }

        [KRPCProperty]
        public float MaxPosition {
            get { return servo.MaxPosition; }
            set { servo.MaxPosition = value; }
        }

        [KRPCProperty]
        public float ConfigSpeed {
            get { return servo.ConfigSpeed; }
        }

        [KRPCProperty]
        public float Speed {
            get { return servo.Speed; }
            set { servo.Speed = value; }
        }

        [KRPCProperty]
        public float CurrentSpeed {
            get { return servo.CurrentSpeed; }
            set { servo.CurrentSpeed = value; }
        }

        [KRPCProperty]
        public float Acceleration {
            get { return servo.Acceleration; }
            set { servo.Acceleration = value; }
        }

        [KRPCProperty]
        public bool IsMoving {
            get { return servo.IsMoving; }
        }

        [KRPCProperty]
        public bool IsFreeMoving {
            get { return servo.IsFreeMoving; }
        }

        [KRPCProperty]
        public bool IsLocked {
            get { return servo.IsLocked; }
            set { servo.IsLocked = value; }
        }

        [KRPCProperty]
        public bool IsAxisInverted {
            get { return servo.IsAxisInverted; }
            set { servo.IsAxisInverted = value; }
        }

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
