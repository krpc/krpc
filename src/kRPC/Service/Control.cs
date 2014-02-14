using System;
using Google.ProtocolBuffers;
using UnityEngine;
using KRPC.Schema.Control;

namespace KRPC.Service
{
	public class Control
	{
		[KRPCMethod]
		public static void Set(ByteString data) {
			var controls = Controls.CreateBuilder ().MergeFrom (data).BuildPartial ();
			if (controls.HasThrottle)
				FlightInputHandler.state.mainThrottle = controls.Throttle;
			if (controls.HasPitch)
				FlightInputHandler.state.pitch = controls.Pitch;
			if (controls.HasRoll)
				FlightInputHandler.state.roll = controls.Roll;
			if (controls.HasYaw)
				FlightInputHandler.state.yaw = controls.Yaw;
			if (controls.HasX)
				FlightInputHandler.state.X = controls.X;
			if (controls.HasY)
				FlightInputHandler.state.Y = controls.Y;
			if (controls.HasZ)
				FlightInputHandler.state.Z = controls.Z;
			if (controls.HasSas)
				FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.SAS, controls.Sas);
			if (controls.HasRcs)
				FlightGlobals.ActiveVessel.ActionGroups.SetGroup (KSPActionGroup.RCS, controls.Rcs);
		}

		[KRPCMethod]
		public static IMessage Get() {
			return Controls.CreateBuilder ()
				.SetThrottle (FlightInputHandler.state.mainThrottle)
				.SetPitch (FlightInputHandler.state.pitch)
				.SetRoll (FlightInputHandler.state.roll)
				.SetYaw (FlightInputHandler.state.yaw)
				.SetX (FlightInputHandler.state.X)
				.SetY (FlightInputHandler.state.Y)
				.SetZ (FlightInputHandler.state.Z)
				.SetSas (FlightInputHandler.state.killRot)
				.SetRcs (FlightInputHandler.RCSLock)
				.BuildPartial ();
		}

		[KRPCMethod]
		public static IMessage GetCurrentStage() {
			return CurrentStage.CreateBuilder ()
				.SetStage ((uint)Staging.CurrentStage) // TODO: is this cast safe?
				.BuildPartial ();
		}

		[KRPCMethod]
		public static void ActivateNextStage() {
			Staging.ActivateNextStage();
		}
	}
}

