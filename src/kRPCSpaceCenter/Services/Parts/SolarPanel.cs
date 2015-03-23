using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KRPCSpaceCenter.ExtensionMethods;

namespace KRPCSpaceCenter.Services.Parts
{
    [KRPCEnum (Service = "SpaceCenter")]
    public enum SolarPanelState
    {
        Extended,
        Retracted,
        Extending,
        Retracting,
        Broken
    }

    [KRPCClass (Service = "SpaceCenter")]
    public sealed class SolarPanel : Equatable<SolarPanel>
    {
        readonly Part part;
        readonly ModuleDeployableSolarPanel panel;

        internal SolarPanel (Part part)
        {
            this.part = part;
            panel = part.InternalPart.Module<ModuleDeployableSolarPanel> ();
            if (panel == null)
                throw new ArgumentException ("Part does not have a ModuleDeployableSolarPanel PartModule");
        }

        public override bool Equals (SolarPanel obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        [KRPCProperty]
        public bool Deployed {
            get {
                return panel.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED || panel.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDING;
            }
            set {
                if (value)
                    panel.Extend ();
                else
                    panel.Retract ();
            }
        }

        [KRPCProperty]
        public SolarPanelState State {
            get {
                switch (panel.panelState) {
                case ModuleDeployableSolarPanel.panelStates.EXTENDED:
                    return SolarPanelState.Extended;
                case ModuleDeployableSolarPanel.panelStates.RETRACTED:
                    return SolarPanelState.Retracted;
                case ModuleDeployableSolarPanel.panelStates.EXTENDING:
                    return SolarPanelState.Extending;
                case ModuleDeployableSolarPanel.panelStates.RETRACTING:
                    return SolarPanelState.Retracting;
                case ModuleDeployableSolarPanel.panelStates.BROKEN:
                    return SolarPanelState.Broken;
                default:
                    throw new ArgumentException ("Unsupported solar panel state");
                }
            }
        }

        [KRPCProperty]
        public float EnergyFlow {
            get { return panel.flowRate; }
        }

        [KRPCProperty]
        public float SunExposure {
            get { return panel.sunAOA; }
        }
    }
}
