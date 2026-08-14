using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class GameModeExtensions
    {
        public static GameMode ToGameMode(this Game.Modes mode)
        {
            switch (mode) {
                case Game.Modes.CAREER:
                    return GameMode.Career;
                case Game.Modes.SANDBOX:
                    return GameMode.Sandbox;
                case Game.Modes.SCENARIO:
                    return GameMode.Scenario;
                case Game.Modes.SCENARIO_NON_RESUMABLE:
                    return GameMode.ScenarioNonResumable;
                case Game.Modes.SCIENCE_SANDBOX:
                    return GameMode.ScienceSandbox;
                case Game.Modes.MISSION:
                    return GameMode.Mission;
                case Game.Modes.MISSION_BUILDER:
                    return GameMode.MissionBuilder;
                default:
                    throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }
    }
}
