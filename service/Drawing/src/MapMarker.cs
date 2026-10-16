using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Service.StdLib;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using KSP.UI.Screens.Mapview;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A marker drawn in the map view, with one of the game's own map icons. Created using
    /// <see cref="Drawing.AddMapMarker" />.
    /// </summary>
    [KRPCClass (Service = "Drawing", GameScene = GameScene.Flight | GameScene.TrackingStation)]
    public class MapMarker : IDrawable
    {
        // The size of the icon in pixels, as for the game's own site markers
        const int pixelSize = 24;

        readonly MapNode node;
        // Whether the marker has been taken out of the scene. The game tears a game object down
        // at the end of the frame, so this records that it is going, and a marker removed and
        // read again in the same frame reports it gone
        bool removed;
        Vector3d position;
        MapMarkerIcon icon;
        // The image the node is showing in place of its icon
        Image shownImage;

        internal MapMarker (Vector3d markerPosition, ReferenceFrame referenceFrame, MapMarkerIcon markerIcon, string caption, bool visible)
        {
            position = markerPosition;
            ReferenceFrame = referenceFrame;
            icon = markerIcon;
            Caption = caption;
            Visible = visible;
            Color = new ColorRGB (1, 1, 1);
            node = MapNode.Create ("KRPC.Drawing.MapMarker", UnityEngine.Color.white, pixelSize, true, true, false);
            node.OnUpdatePosition += OnUpdatePosition;
            node.OnUpdateVisible += OnUpdateVisible;
            node.OnUpdateCaption += OnUpdateCaption;
            node.OnUpdateType += OnUpdateType;
            GameObject = node.gameObject;
            Addon.AddObject (this);
        }

        // The game positions a map node in scaled space
        Vector3d OnUpdatePosition (MapNode mapNode)
        {
            if (!CanBeDrawn)
                return Vector3d.zero;
            return ScaledSpace.LocalToScaledSpace (ReferenceFrame.PositionToWorldSpace (position));
        }

        void OnUpdateVisible (MapNode mapNode, MapNode.IconData iconData)
        {
            iconData.visible = Visible && CanBeDrawn;
            iconData.iconEnabled = true;
            iconData.color = Color.ToColor ();
        }

        void OnUpdateCaption (MapNode mapNode, MapNode.CaptionData captionData)
        {
            captionData.Header = Caption;
        }

        void OnUpdateType (MapNode mapNode, MapNode.TypeData typeData)
        {
            SetType (typeData, icon);
        }

        static void SetType (MapNode.TypeData typeData, MapMarkerIcon icon)
        {
            switch (icon) {
            case MapMarkerIcon.Generic:
                typeData.oType = MapObject.ObjectType.Generic;
                break;
            case MapMarkerIcon.CelestialBody:
                typeData.oType = MapObject.ObjectType.CelestialBody;
                break;
            case MapMarkerIcon.Apoapsis:
                typeData.oType = MapObject.ObjectType.Apoapsis;
                break;
            case MapMarkerIcon.Periapsis:
                typeData.oType = MapObject.ObjectType.Periapsis;
                break;
            case MapMarkerIcon.AscendingNode:
                typeData.oType = MapObject.ObjectType.AscendingNode;
                break;
            case MapMarkerIcon.DescendingNode:
                typeData.oType = MapObject.ObjectType.DescendingNode;
                break;
            case MapMarkerIcon.ManeuverNode:
                typeData.oType = MapObject.ObjectType.ManeuverNode;
                break;
            case MapMarkerIcon.Encounter:
                SetPatchTransition (typeData, MapNode.PatchTransitionNodeType.Encounter);
                break;
            case MapMarkerIcon.Escape:
                SetPatchTransition (typeData, MapNode.PatchTransitionNodeType.Escape);
                break;
            case MapMarkerIcon.Impact:
                SetPatchTransition (typeData, MapNode.PatchTransitionNodeType.Impact);
                break;
            case MapMarkerIcon.CloseApproach:
                SetApproach (typeData, MapNode.ApproachNodeType.CloseApproachOwn);
                break;
            case MapMarkerIcon.Intersect:
                SetApproach (typeData, MapNode.ApproachNodeType.IntersectOwn);
                break;
            case MapMarkerIcon.Ship:
                SetVessel (typeData, VesselType.Ship);
                break;
            case MapMarkerIcon.Probe:
                SetVessel (typeData, VesselType.Probe);
                break;
            case MapMarkerIcon.Relay:
                SetVessel (typeData, VesselType.Relay);
                break;
            case MapMarkerIcon.Station:
                SetVessel (typeData, VesselType.Station);
                break;
            case MapMarkerIcon.Base:
                SetVessel (typeData, VesselType.Base);
                break;
            case MapMarkerIcon.Lander:
                SetVessel (typeData, VesselType.Lander);
                break;
            case MapMarkerIcon.Rover:
                SetVessel (typeData, VesselType.Rover);
                break;
            case MapMarkerIcon.Plane:
                SetVessel (typeData, VesselType.Plane);
                break;
            case MapMarkerIcon.EVA:
                SetVessel (typeData, VesselType.EVA);
                break;
            case MapMarkerIcon.Flag:
                SetVessel (typeData, VesselType.Flag);
                break;
            case MapMarkerIcon.Debris:
                SetVessel (typeData, VesselType.Debris);
                break;
            case MapMarkerIcon.SpaceObject:
                SetVessel (typeData, VesselType.SpaceObject);
                break;
            case MapMarkerIcon.DeployedScience:
                SetVessel (typeData, VesselType.DeployedScienceController);
                break;
            case MapMarkerIcon.LaunchSite:
                SetSite (typeData, MapNode.SiteType.LaunchSite);
                break;
            case MapMarkerIcon.Runway:
                SetSite (typeData, MapNode.SiteType.Runway);
                break;
            case MapMarkerIcon.KSC:
                SetSite (typeData, MapNode.SiteType.KSC);
                break;
            default:
                throw new ArgumentOutOfRangeException (nameof (icon));
            }
        }

        static void SetPatchTransition (MapNode.TypeData typeData, MapNode.PatchTransitionNodeType type)
        {
            typeData.oType = MapObject.ObjectType.PatchTransition;
            typeData.pType = type;
        }

        static void SetApproach (MapNode.TypeData typeData, MapNode.ApproachNodeType type)
        {
            typeData.oType = MapObject.ObjectType.ApproachIntersect;
            typeData.aType = type;
        }

        static void SetVessel (MapNode.TypeData typeData, VesselType type)
        {
            typeData.oType = MapObject.ObjectType.Vessel;
            typeData.vType = type;
        }

        static void SetSite (MapNode.TypeData typeData, MapNode.SiteType type)
        {
            typeData.oType = MapObject.ObjectType.Site;
            typeData.sType = type;
        }

        bool CanBeDrawn {
            get {
                return GameObjectState == GameObjectState.Live &&
                ReferenceFrame.GameObjectState == GameObjectState.Live;
            }
        }

        /// <summary>
        /// Update the map node, once the camera has moved for the frame. A map node is
        /// updated by the object it marks, which here is the marker.
        /// </summary>
        public void LateUpdate ()
        {
            if (GameObjectState == GameObjectState.Live && View.Scaled)
                node.NodeUpdate ();
        }

        /// <summary>
        /// Update the image shown in place of the icon. The rest of the map node is updated
        /// through its callbacks.
        /// </summary>
        public void Update ()
        {
            var image = Image.IsLive (Image) ? Image : null;
            if (ReferenceEquals (image, shownImage))
                return;
            if (image != null) {
                node.SetIcon (image.Sprite);
            } else {
                var typeData = new MapNode.TypeData (MapObject.ObjectType.Generic);
                SetType (typeData, icon);
                node.SetIcon (node.GetIconIndex (typeData));
            }
            shownImage = image;
        }

        /// <summary>
        /// Destroy the marker.
        /// </summary>
        public void Destroy ()
        {
            removed = true;
            if (node != null)
                node.Terminate ();
        }

        /// <summary>
        /// The game object for the marker.
        /// </summary>
        public GameObject GameObject { get; private set; }

        /// <summary>
        /// The state of the marker, which is the map node it was made with. Removing it,
        /// clearing the drawing objects, disconnecting the client that made it and leaving
        /// the scene each destroy that node.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return removed || GameObject == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        void CheckExists ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                throw new ObjectDestroyedException (
                    "The map marker no longer exists, as it has been removed.");
        }

        /// <summary>
        /// Remove the marker.
        /// </summary>
        [KRPCMethod]
        public void Remove ()
        {
            CheckExists ();
            Addon.RemoveObject (this);
        }

        /// <summary>
        /// Reference frame that the position of the marker is in.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// Whether the marker is visible.
        /// </summary>
        [KRPCProperty]
        public bool Visible { get; set; }

        /// <summary>
        /// Position of the marker.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Position {
            get { return position.ToTuple (); }
            set { position = value.ToVector (); }
        }

        /// <summary>
        /// The icon that the marker is drawn with.
        /// </summary>
        [KRPCProperty]
        public MapMarkerIcon Icon {
            get { return icon; }
            set {
                CheckExists ();
                SetType (new MapNode.TypeData (MapObject.ObjectType.Generic), value);
                icon = value;
                // The node sets the icon of its new type, so an image is shown again over it
                shownImage = null;
            }
        }

        /// <summary>
        /// The image that the marker is drawn with in place of its icon, or null to draw the
        /// icon.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Image Image { get; set; }

        /// <summary>
        /// The caption shown when the mouse is over the marker.
        /// </summary>
        [KRPCProperty]
        public string Caption { get; set; }

        /// <summary>
        /// The color of the marker.
        /// </summary>
        [KRPCProperty]
        public ColorRGB Color { get; set; }
    }
}
