using System;
using Brutal.ImGuiApi;
using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A horizontal slider. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Slider : Widget
    {
        string label;
        float value;
        float minimum;
        float maximum;

        internal Slider (string label, float minimum, float maximum, bool visible) : base (visible)
        {
            if (!(minimum < maximum))
                throw new ArgumentException ("The minimum must be less than the maximum");
            Label = label;
            this.minimum = minimum;
            this.maximum = maximum;
            value = minimum;
        }

        /// <summary>
        /// The text shown next to the slider.
        /// </summary>
        [KRPCProperty]
        public string Label {
            get { return label; }
            set { label = value ?? throw new ArgumentNullException (nameof (value)); }
        }

        /// <summary>
        /// The value of the slider, clamped to its range.
        /// </summary>
        [KRPCProperty]
        public float Value {
            get { return value; }
            set { this.value = Math.Clamp (value, minimum, maximum); }
        }

        /// <summary>
        /// The minimum value.
        /// </summary>
        /// <remarks>Throws an exception if set to a value not less than the maximum.</remarks>
        [KRPCProperty]
        public float Minimum {
            get { return minimum; }
            set {
                if (!(value < maximum))
                    throw new ArgumentOutOfRangeException (nameof (Minimum));
                minimum = value;
                Value = this.value;
            }
        }

        /// <summary>
        /// The maximum value.
        /// </summary>
        /// <remarks>Throws an exception if set to a value not greater than the minimum.</remarks>
        [KRPCProperty]
        public float Maximum {
            get { return maximum; }
            set {
                if (!(value > minimum))
                    throw new ArgumentOutOfRangeException (nameof (Maximum));
                maximum = value;
                Value = this.value;
            }
        }

        /// <summary>
        /// Whether the value has been changed by dragging the slider.
        /// </summary>
        /// <remarks>
        /// A change sets this to <c>true</c>, and it stays <c>true</c> until set to
        /// <c>false</c>. Setting <see cref="Value"/> leaves it unchanged.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }

        internal override void DrawWidget ()
        {
            if (ImGui.SliderFloat (ItemLabel (label), ref value, minimum, maximum))
                Changed = true;
            RecordItem ();
        }
    }
}
