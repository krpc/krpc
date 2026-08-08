using System;
using KRPC.Service.Attributes;
using UnityEngine;

namespace KRPC.UI
{
    /// <summary>
    /// A control for choosing a number by dragging a handle along a track.
    /// Added to a <see cref="Canvas" /> or a <see cref="Panel" />.
    /// </summary>
    [KRPCClass (Service = "UI")]
    public class Slider : Control
    {
        /// <summary>
        /// How wide the handle is drawn, in pixels.
        /// </summary>
        const float handleWidth = 20;

        readonly UnityEngine.UI.Slider slider;

        /// <summary>
        /// Whether the slider is being given a new range, during which the value it is
        /// notified of is one that Unity clamped, not one the user chose.
        /// </summary>
        bool settingRange;

        internal Slider (GameObject parent, bool vertical, bool visible)
            : base (Widgets.Create (parent, "krpc.slider",
                vertical ? 20 : 160, vertical ? 160 : 20), visible)
        {
            Widgets.AddImage (
                Widgets.CreateFilling (GameObject, "krpc.slider.track", 0),
                Widgets.Style (
                    skin => vertical ? skin.verticalSlider : skin.horizontalSlider));

            // The track the handle slides along is shortened by half a handle at each end,
            // so that the handle stays within the slider at either extreme.
            var slideArea = Widgets.CreateFilling (GameObject, "krpc.slider.slideArea", 0);
            var slideRect = slideArea.GetComponent<UnityEngine.RectTransform> ();
            slideRect.offsetMin = vertical
                ? new Vector2 (0, handleWidth / 2) : new Vector2 (handleWidth / 2, 0);
            slideRect.offsetMax = vertical
                ? new Vector2 (0, -handleWidth / 2) : new Vector2 (-handleWidth / 2, 0);

            var handle = Widgets.CreateFilling (slideArea, "krpc.slider.handle", 0);
            var handleRect = handle.GetComponent<UnityEngine.RectTransform> ();
            // Unity positions the handle by moving its anchors along the track, so it is
            // anchored to a point along the slider's direction and given a fixed size
            // across it.
            handleRect.anchorMin = new Vector2 (0, 0);
            handleRect.anchorMax = vertical ? new Vector2 (1, 0) : new Vector2 (0, 1);
            handleRect.sizeDelta = vertical
                ? new Vector2 (0, handleWidth) : new Vector2 (handleWidth, 0);
            var thumb = Widgets.Style (
                skin => vertical ? skin.verticalSliderThumb : skin.horizontalSliderThumb);
            var handleImage = Widgets.AddImage (handle, thumb);

            slider = GameObject.AddComponent<UnityEngine.UI.Slider> ();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = vertical
                ? UnityEngine.UI.Slider.Direction.BottomToTop
                : UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            Widgets.AddTransition (slider, thumb);
            slider.onValueChanged.AddListener (x => {
                if (!settingRange)
                    Changed = true;
            });
        }

        /// <inheritdoc />
        protected override UnityEngine.UI.Selectable Selectable {
            get { return slider; }
        }

        /// <summary>
        /// The value of the slider, between <see cref="Min" /> and <see cref="Max" />.
        /// Setting a value outside the range is refused.
        /// </summary>
        [KRPCProperty]
        public float Value {
            get { return slider.value; }
            set {
                // Unity moves a value outside the range to the nearest end of it, so a
                // client that set the wrong value would be left reading back one it did
                // not set and no way to tell that it had happened.
                if (value < slider.minValue || value > slider.maxValue)
                    throw new ArgumentOutOfRangeException (
                        nameof (value), "The value is outside the range of the slider");
                // Unity notifies its listeners of a value set by a client just as it does
                // one the user chose, so the value is set without notifying them.
                slider.SetValueWithoutNotify (value);
            }
        }

        /// <summary>
        /// The value at the low end of the slider, which is its left hand end, or the
        /// bottom of a vertical slider. Must not be greater than <see cref="Max" />.
        /// </summary>
        [KRPCProperty]
        public float Min {
            get { return slider.minValue; }
            set { SetRange (value, slider.maxValue); }
        }

        /// <summary>
        /// The value at the high end of the slider, which is its right hand end, or the
        /// top of a vertical slider. Must not be less than <see cref="Min" />.
        /// </summary>
        [KRPCProperty]
        public float Max {
            get { return slider.maxValue; }
            set { SetRange (slider.minValue, value); }
        }

        /// <summary>
        /// Give the slider a new range. Unity clamps the value to it and notifies its
        /// listeners of the value it arrives at, which is not one the user chose.
        /// </summary>
        void SetRange (float min, float max)
        {
            // A range with its ends crossed refuses every value, including the one the
            // slider is showing, so it is refused rather than handed to Unity to make
            // something of.
            if (min > max)
                throw new ArgumentException (
                    "The minimum of the range must not be greater than the maximum");
            settingRange = true;
            try {
                slider.minValue = min;
                slider.maxValue = max;
            } finally {
                settingRange = false;
            }
        }

        /// <summary>
        /// Whether the slider has been changed.
        /// </summary>
        /// <remarks>
        /// This property is set to true when the user moves the slider, and not when a
        /// client sets its value.
        /// A client script should reset the property to false in order to detect subsequent changes.
        /// </remarks>
        [KRPCProperty]
        public bool Changed { get; set; }
    }
}
