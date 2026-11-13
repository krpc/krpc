using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Fine tune the closest approach to the target.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationCourseCorrection : Operation
    {
        static Field<object, double> courseCorrectFinalPeA;
        static Field<object, double> interceptDistance;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationCourseCorrection");
            courseCorrectFinalPeA = Bindings.EditableDouble (type, "CourseCorrectFinalPeA");
            interceptDistance = Bindings.EditableDouble (type, "InterceptDistance");
        }

        internal OperationCourseCorrection () : base ("OperationCourseCorrection")
        {
        }

        /// <summary>
        /// The periapsis altitude at a target body, in meters.
        /// </summary>
        [KRPCProperty]
        public double CourseCorrectFinalPeA {
            get { return courseCorrectFinalPeA.Get (Instance); }
            set { courseCorrectFinalPeA.Set (Instance, value); }
        }

        /// <summary>
        /// The closest approach distance to a target vessel, in meters.
        /// </summary>
        [KRPCProperty]
        public double InterceptDistance {
            get { return interceptDistance.Get (Instance); }
            set { interceptDistance.Set (Instance, value); }
        }
    }
}
