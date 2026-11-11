using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Util {
	public class Vector6 {
		[KRPCEnum(Service = "MechJeb")]
		public enum Direction {
			Forward,
			Back,
			Up,
			Down,
			Right,
			Left
		}
	}
}
