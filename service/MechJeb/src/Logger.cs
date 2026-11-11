using System;

using UnityLogger = UnityEngine.Debug;

namespace KRPC.MechJeb {
	public static class Logger {
		public static void Debug(string message) {
#if DEBUG
			Info(message);
#endif
		}

		public static void Info(string message) {
			UnityLogger.Log("[KRPC.MechJeb] " + message);
		}

		public static void Warning(string message) {
			UnityLogger.LogWarning("[KRPC.MechJeb] " + message);
		}

		public static void Warning(string message, Exception ex) {
			Warning(message);
			UnityLogger.LogException(ex);
		}

		public static void Severe(string message) {
			UnityLogger.LogError("[KRPC.MechJeb] " + message);
		}

		public static void Severe(string message, Exception ex) {
			Severe(message);
			UnityLogger.LogException(ex);
		}
	}
}
