#pragma warning disable 1591

namespace KRPC.SpaceCenter.NameTag
{
    public static class Career
    {
        /// <summary>
        /// Detect whether we'll allow you to add a NameTag to a part during the editor.
        /// You can always add one post-editor during flight, but during editing it will
        /// depend on building level.<br/>
        /// NOTE: This method does NOT have an associated suffix because it is meant to be called
        /// from inside the editor, when a script won't be running.
        /// </summary>
        /// <param name="whichEditor">Pass in whether you are checking from the VAB or SPH.</param>
        /// <param name="reason">returns a string describing what would need upgrading to change the answer.</param>
        /// <returns>true if it can. false if it cannot.</returns>
        public static bool CanTagInEditor(EditorFacility whichEditor, out string reason)
        {
            float buildingLevel;
            switch (whichEditor)
            {
                case EditorFacility.VAB:
                    reason = "vehicle assembly building";
                    buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.VehicleAssemblyBuilding);
                    break;
                case EditorFacility.SPH:
                    reason = "space plane hangar";
                    buildingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.SpaceplaneHangar);
                    break;
                default:
                    reason = "unknown editor building";
                    return false; // not even sure how you could get here.
            }
            // We'll attach it to the same point where the game starts unlocking basic action groups:
            return GameVariables.Instance.UnlockedActionGroupsStock(buildingLevel, false);
        }

        /// <summary>
        /// Same as CanTagInEditor, but without the 'reason' parameter.  (This is a separate method
        /// only because you can't default out parameters like 'out string reason' to make them optional.)
        /// </summary>
        /// <returns>true if you can. false if you cannot.</returns>
        public static bool CanTagInEditor(EditorFacility whichEditor)
        {
            string dummy;
            return CanTagInEditor(whichEditor, out dummy);
        }
    }
}
