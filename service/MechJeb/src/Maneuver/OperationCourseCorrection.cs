using System;
using System.Reflection;

using KRPC.MechJeb.ExtensionMethods;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver {
	/// <summary>
	/// Create a maneuver to fine-tune closest approach to target
	/// </summary>
	[KRPCClass(Service = "MechJeb")]
	public class OperationCourseCorrection : Operation {
		internal new const string MechJebType = "MuMech.OperationCourseCorrection";

		// Fields and methods
		private static FieldInfo courseCorrectFinalPeAField;
		private static FieldInfo interceptDistanceField;

		// Instance objects
		private object courseCorrectFinalPeA;
		private object interceptDistance;

		internal static new void InitType(Type type) {
			courseCorrectFinalPeAField = type.GetCheckedField("courseCorrectFinalPeA");
			interceptDistanceField = type.GetCheckedField("interceptDistance");
		}

		protected internal override void InitInstance(object instance) {
			base.InitInstance(instance);

			this.courseCorrectFinalPeA = courseCorrectFinalPeAField.GetInstanceValue(instance);
			this.interceptDistance = interceptDistanceField.GetInstanceValue(instance);
		}

		[KRPCProperty]
		public double CourseCorrectFinalPeA {
			get => EditableDouble.Get(this.courseCorrectFinalPeA);
			set => EditableDouble.Set(this.courseCorrectFinalPeA, value);
		}

		[KRPCProperty]
		public double InterceptDistance {
			get => EditableDouble.Get(this.interceptDistance);
			set => EditableDouble.Set(this.interceptDistance, value);
		}
	}
}
