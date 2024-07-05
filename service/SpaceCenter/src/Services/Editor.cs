using System;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.Services
{
    /// <summary>
    /// VAB service.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Editor)]
    public class Editor
    { 

        // N.B. these functions are not `readonly` parameters as there's a (small)
        // possibility their values may change over time.
        internal EditorLogic GetEditor()
        {
            return GameObject.Find("EditorLogic").GetComponent<EditorLogic>();
        }

        internal EditorDriver GetEditorDriver()
        {
            return GameObject.Find("EditorLogic").GetComponent<EditorDriver>();
        }

        /// <summary>
        /// Gets the current ship.
        /// </summary>
        [KRPCProperty(Nullable = true)]
        public EditorShip CurrentShip
        {
            get
            {
                if (GetEditor().ship == null)
                {
                    return null;
                }
                return new EditorShip(GetEditor().ship);
            }
        }

        /// <summary>
        /// Switches between the VAB and SPH.
        /// </summary>
        [KRPCProcedure]
        public void SwitchEditor()
        {
            GetEditor().SwitchEditor();
        }

        /// <summary>
        /// Launches the vessel from the provided site.
        /// </summary>
        /// <param name="siteName">Site name. If invalid, the default for the
        /// current editor will be used.</param>
        [KRPCProcedure]
        public void LaunchVessel(string siteName)
        {
            GetEditor().launchVessel(siteName);
        }

        /// <summary>
        /// Gets the current editor facility.
        /// </summary>
        public EditorFacility CurrentFacility
        {
            get
            {
                switch (EditorDriver.editorFacility)
                {
                    case global::EditorFacility.VAB:
                        return EditorFacility.VAB;
                    case global::EditorFacility.SPH:
                        return EditorFacility.SPH;
                    default:
                        return EditorFacility.None;
                }
            }
        }
    }
}