#pragma warning disable 1591

using UnityEngine;

namespace KRPC.SpaceCenter.NameTag
{
    // Note: name kept as KOSNameTag for backward compatibility
    public class KOSNameTag : PartModule
    {
        private Window typingWindow;

        [KSPField(isPersistant = true,
                  guiActive = true,
                  guiActiveEditor = true,
                  guiName = "name tag")]
        public string nameTag = "";

        [KSPEvent(guiActive = true,
                  guiActiveEditor = true,
                  guiName = "Change Name Tag")]
        public void PopupNameTagChanger()
        {
            if (typingWindow != null)
                typingWindow.Close();
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorFacility whichEditor = EditorLogic.fetch.ship.shipFacility;
                if (!(Career.CanTagInEditor(whichEditor)))
                {
                    var formattedString = string.Format("The {0} requires an upgrade to assign name tags", whichEditor);
                    ScreenMessages.PostScreenMessage(formattedString, 6, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }
            typingWindow = gameObject.AddComponent<Window>();
            typingWindow.Invoke(this, nameTag);
        }

        public void TypingDone(string newValue)
        {
            nameTag = newValue;
            TypingCancel();
        }

        public void TypingCancel()
        {
            typingWindow.Close();
            Destroy(typingWindow);
            typingWindow = null;
        }
    }
}
