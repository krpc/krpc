using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class GameModeExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static GameMode ToGameMode(this Game.Modes mode)
        {
            switch (mode) {
                case Game.Modes.CAREER:
                    return GameMode.Career;
                case Game.Modes.MISSION:
                    return GameMode.Mission;
                case Game.Modes.MISSION_BUILDER:
                    return GameMode.MissionBuilder;
                case Game.Modes.SANDBOX:
                    return GameMode.Sandbox;
                case Game.Modes.SCENARIO:
                    return GameMode.Scenario;
                case Game.Modes.SCENARIO_NON_RESUMABLE:
                    return GameMode.ScenarioNonResumable;
                case Game.Modes.SCIENCE_SANDBOX:
                    return GameMode.ScienceSandbox;
                default:
                    throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }
    }
}
