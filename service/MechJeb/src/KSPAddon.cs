using UnityEngine;

namespace KRPC.MechJeb {
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	internal class InitTypes : MonoBehaviour {
		public void Start() {
			Logger.Info("Loading MechJeb types...");
			Logger.Info(MechJeb.InitTypes() ? "MechJeb found!" : "MechJeb is not available.");
			MechJeb.ShowErrors();
		}
	}

	[KSPAddon(KSPAddon.Startup.Flight, false)]
	internal class InitInstance : MonoBehaviour {
		private Vessel activeVessel;

		public void LateUpdate() {
			if(!MechJeb.TypesLoaded)
				return;

			// Refresh MechJeb instance when focus changes or a flight is reverted to launch.
			if(this.activeVessel != FlightGlobals.ActiveVessel) {
				this.activeVessel = FlightGlobals.ActiveVessel;
				Logger.Info("Initializing MechJeb instance...");
				Logger.Info(MechJeb.InitInstance() ? "KRPC.MechJeb is ready!" : "MechJeb found but the instance initialization wasn't successful. Maybe you don't have any MechJeb part attached to the vessel?");
				MechJeb.ShowErrors();
			}
		}
	}
}
