using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.SpaceCenter.Services;
using KRPC.Utils;
using UnityEngine;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;
using Tuple3 = System.Tuple<double, double, double>;

namespace KRPC.Drawing
{
    /// <summary>
    /// A marker drawn on the navball. Created using <see cref="Drawing.AddNavballMarker" />.
    /// </summary>
    [KRPCClass (Service = "Drawing")]
    public class NavballMarker : IDrawable
    {
        // The game database directory holding the icons a marker can be drawn with
        const string iconDirectory = "Squad/Contracts/Icons/";

        // The game object for one of the navball's own markers, which is cloned to make a new one.
        const string stockMarkerName = "ProgradeVector";

        // The rotation at which a marker's mesh faces the navball camera.
        static readonly Quaternion stockRotation = Quaternion.Euler (90f, 180f, 0f);

        static IList<string> availableIcons;

        readonly KSP.UI.Screens.Flight.NavBall navBall;
        readonly UnityEngine.Material material;
        // The scale at which the game draws the navball's own markers, which a size of 1 matches.
        readonly Vector3 stockScale;
        // Whether the marker has been taken out of the scene. The game tears a game object down
        // at the end of the frame, so this records that it is going, and a marker removed and
        // read again in the same frame reports it gone
        bool removed;
        Vector3d direction;
        Tuple3 color;
        string icon;
        float size;

        internal NavballMarker (Vector3d markerDirection, ReferenceFrame referenceFrame, bool visible)
        {
            navBall = UnityEngine.Object.FindObjectOfType<KSP.UI.Screens.Flight.NavBall> ();
            if (navBall == null)
                throw new InvalidOperationException ("Navball not found");
            var stockMarker = FindStockMarker (navBall);
            stockScale = stockMarker.transform.localScale;

            // Cloning one of the navball's own markers gives a marker with the game's mesh,
            // shader and render layer, drawn by the navball camera along with the rest.
            GameObject = UnityEngine.Object.Instantiate (stockMarker);
            GameObject.name = "KRPC.Drawing.NavballMarker";
            GameObject.transform.SetParent (stockMarker.transform.parent, false);
            var renderer = GameObject.GetComponent<Renderer> ();
            // Give the clone a material of its own, so that changing it leaves the navball's
            // own markers alone.
            material = new UnityEngine.Material (renderer.sharedMaterial);
            renderer.material = material;
            // The navball's own markers share a texture atlas, each sampling a sub-rect of it.
            // A marker's icon is a texture in its own right, so it uses the whole of one.
            material.SetTextureOffset ("_MainTexture", Vector2.zero);
            material.SetTextureScale ("_MainTexture", Vector2.one);

            direction = markerDirection;
            ReferenceFrame = referenceFrame;
            Visible = visible;
            Color = new Tuple3 (1, 1, 1);
            Icon = "default";
            Size = 1f;
            Addon.AddObject (this);
        }

        // The navball's own markers are siblings, under the parent of the navball itself.
        static GameObject FindStockMarker (KSP.UI.Screens.Flight.NavBall navBall)
        {
            foreach (var child in navBall.transform.parent.GetComponentsInChildren<Transform> (true)) {
                if (child.gameObject.name == stockMarkerName)
                    return child.gameObject;
            }
            throw new InvalidOperationException ("Navball marker not found");
        }

        /// <summary>
        /// Update the marker.
        /// </summary>
        public void Update ()
        {
            // A marker whose frame is defined against something the game has destroyed is left
            // where it is, so the client can point it at another frame
            if (GameObjectState != GameObjectState.Live ||
                ReferenceFrame.GameObjectState != GameObjectState.Live) {
                GameObject.SetActive (false);
                return;
            }
            Vector3 worldDirection = ReferenceFrame.DirectionToWorldSpace (direction).normalized;
            var position = navBall.attitudeGymbal * (worldDirection * navBall.VectorUnitScale);
            GameObject.transform.localPosition = position;
            // The marker faces the navball camera wherever on the ball it sits, so its rotation
            // is fixed in world space
            GameObject.transform.rotation = stockRotation;
            // A marker on the far side of the ball is hidden, and one near the edge fades out,
            // as the navball's own markers do.
            GameObject.SetActive (Visible && position.z > navBall.VectorUnitCutoff);
            material.SetFloat ("_Opacity", Mathf.Clamp01 (Vector3.Dot (position.normalized, Vector3.forward)));
        }

        /// <summary>
        /// Destroy the marker.
        /// </summary>
        public void Destroy ()
        {
            removed = true;
            UnityEngine.Object.Destroy (material);
            UnityEngine.Object.Destroy (GameObject);
        }

        /// <summary>
        /// The game object for the marker.
        /// </summary>
        public GameObject GameObject { get; private set; }

        /// <summary>
        /// The state of the marker, which is the game object it was made with. Removing it,
        /// clearing the drawing objects, disconnecting the client that made it and leaving
        /// the scene each destroy that object.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                return removed || GameObject == null
                    ? GameObjectState.Destroyed : GameObjectState.Live;
            }
        }

        // The marker's own material, checked to still exist. Every member that reaches into
        // the game goes through this or through the checked game object.
        UnityEngine.Material Material {
            get {
                CheckExists ();
                return material;
            }
        }

        void CheckExists ()
        {
            if (GameObjectState == GameObjectState.Destroyed)
                throw new ObjectDestroyedException (
                    "The navball marker no longer exists, as it has been removed.");
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
        /// Reference frame that the direction of the marker is in.
        /// </summary>
        [KRPCProperty]
        public ReferenceFrame ReferenceFrame { get; set; }

        /// <summary>
        /// Whether the marker is visible.
        /// </summary>
        /// <remarks>
        /// A visible marker is still hidden while it is on the far side of the navball.
        /// </remarks>
        [KRPCProperty]
        public bool Visible { get; set; }

        /// <summary>
        /// Direction that the marker is drawn at.
        /// </summary>
        [KRPCProperty]
        public Tuple3 Direction {
            get { return direction.ToTuple (); }
            set { direction = value.ToVector (); }
        }

        /// <summary>
        /// Set the color
        /// </summary>
        [KRPCProperty]
        public Tuple3 Color {
            get { return color; }
            set {
                color = value;
                Material.SetColor ("_TintColor", color.ToColor ());
            }
        }

        /// <summary>
        /// The icon that the marker is drawn with.
        /// See <see cref="AvailableIcons"/> for the icons that can be used.
        /// </summary>
        [KRPCProperty]
        public string Icon {
            get { return icon; }
            set {
                if (!AvailableIcons ().Contains (value))
                    throw new ArgumentException ("Icon does not exist");
                icon = value;
                Material.SetTexture ("_MainTexture", GameDatabase.Instance.GetTexture (iconDirectory + icon, false));
            }
        }

        /// <summary>
        /// Size of the marker, relative to the size of the navball's own markers.
        /// </summary>
        [KRPCProperty]
        public float Size {
            get { return size; }
            set {
                size = value;
                CheckExists ();
                GameObject.transform.localScale = stockScale * size;
            }
        }

        /// <summary>
        /// A list of all available icons, taken from "GameData/Squad/Contracts/Icons/".
        /// </summary>
        [KRPCMethod]
        public static IList<string> AvailableIcons ()
        {
            if (availableIcons == null) {
                availableIcons = GameDatabase.Instance.databaseTexture
                    .Where (texture => texture.name.StartsWith (iconDirectory, StringComparison.Ordinal))
                    .Select (texture => texture.name.Substring (iconDirectory.Length))
                    .ToList ();
            }
            return availableIcons;
        }
    }
}
