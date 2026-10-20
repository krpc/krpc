using KRPC.Service.Attributes;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// A color, as red, green and blue components between 0 and 1.
    /// </summary>
    [KRPCStruct (Service = "StdLib")]
    public struct ColorRGB
    {
        /// <summary>
        /// Construct a color from its components.
        /// </summary>
        public ColorRGB (float r, float g, float b) : this ()
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// The red component.
        /// </summary>
        [KRPCProperty]
        public float R { get; set; }

        /// <summary>
        /// The green component.
        /// </summary>
        [KRPCProperty]
        public float G { get; set; }

        /// <summary>
        /// The blue component.
        /// </summary>
        [KRPCProperty]
        public float B { get; set; }
    }
}
