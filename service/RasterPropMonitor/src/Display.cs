using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// A screen page that a script draws, and whose button presses it answers. Obtained by
    /// calling <see cref="Monitor.Display"/> or <see cref="RasterPropMonitor.Display"/>.
    /// </summary>
    /// <remarks>
    /// A monitor shows a display on a page whose handler is <c>KRPCDisplay</c>. kRPC's
    /// optional Module Manager patch adds such a page, named <c>krpc</c>, to every monitor.
    /// The content is kept while KSP builds the IVA again, but not saved with the game.
    /// </remarks>
    [KRPCClass (Service = "RasterPropMonitor", GameScene = GameScene.Flight)]
    public class Display : Equatable<Display>
    {
        readonly DisplayState state;

        internal Display (DisplayState innerState)
        {
            state = innerState;
        }

        /// <summary>
        /// Check that the displays are the same.
        /// </summary>
        public override bool Equals (Display other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (state, other.state);
        }

        /// <summary>
        /// Hash the display.
        /// </summary>
        public override int GetHashCode ()
        {
            return state.GetHashCode ();
        }

        /// <summary>
        /// The name of the display, or <c>null</c> for a monitor's own display.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public string Name {
            get { return state.Name; }
        }

        /// <summary>
        /// The text of the display, in Raster Prop Monitor's page format. It can hold
        /// markup, such as <c>[#ff0000]</c> for a color, and a line can format variables, as
        /// in <c>{0:F0} m $&amp;$ ALTITUDE</c>. Raster Prop Monitor updates the variables as
        /// it draws.
        /// </summary>
        [KRPCProperty]
        public string Text {
            get { return state.Text; }
            set {
                if (value == null)
                    throw new ArgumentNullException (nameof (value));
                state.Text = value;
            }
        }

        /// <summary>
        /// The background image of the display, drawn under the text and scaled to the
        /// screen, as a PNG or JPEG image. <c>null</c> for none.
        /// Throws an exception if the image does not decode.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public byte[] Image {
            get { return state.Image; }
            set { state.Image = value; }
        }

        /// <summary>
        /// Clear the text and the image.
        /// </summary>
        [KRPCMethod]
        public void Clear ()
        {
            state.Text = string.Empty;
            state.Image = null;
        }

        /// <summary>
        /// Whether a monitor shows the display now.
        /// </summary>
        [KRPCProperty]
        public bool Shown {
            get { return state.Shown; }
        }

        /// <summary>
        /// An event that fires when a monitor showing the display has a button pressed.
        /// The event lasts until it is removed.
        /// </summary>
        /// <param name="id">The button's index in <see cref="Monitor.Buttons"/>.</param>
        [KRPCMethod]
        public KRPC.Service.Messages.Event ButtonPressed (int id)
        {
            return CountEvent (() => state.Presses (id));
        }

        /// <summary>
        /// An event that fires when a monitor showing the display has a button released.
        /// The event lasts until it is removed.
        /// </summary>
        /// <param name="id">The button's index in <see cref="Monitor.Buttons"/>.</param>
        [KRPCMethod]
        public KRPC.Service.Messages.Event ButtonReleased (int id)
        {
            return CountEvent (() => state.Releases (id));
        }

        // An event that fires on each update in which the count has grown
        internal static KRPC.Service.Messages.Event CountEvent (Func<int> count)
        {
            var last = count ();
            return new Event (() => {
                var current = count ();
                if (current == last)
                    return false;
                last = current;
                return true;
            }).Message;
        }
    }
}
