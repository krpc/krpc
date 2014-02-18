using System;
using KRPC.Service;
using KRPC.Schema.Control;

namespace TestServer.Services
{
    [KRPCService]
    public class Control
    {
        private static ControlInputs controls;

        [KRPCProcedure]
        public static void SetControlInputs(ControlInputs controls) {
            Control.controls = controls;
        }

        [KRPCProcedure]
        public static ControlInputs GetControlInputs() {
            return Control.controls;
        }

        [KRPCProcedure]
        public static void ActivateNextStage() {
        }
    }
}
