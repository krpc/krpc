using KRPC.Service.Attributes;

namespace KRPC.Service.StdLib
{
    /// <summary>
    /// A color, as red, green, blue and alpha components between 0 and 1.
    /// </summary>
    /// <remarks>
    /// An alpha of 0 is fully transparent and 1 is fully opaque.
    /// </remarks>
    [KRPCStruct (Service = "StdLib")]
    public struct ColorRGBA
    {
        /// <summary>
        /// Construct a color from its components.
        /// </summary>
        public ColorRGBA (float r, float g, float b, float a) : this ()
        {
            R = r;
            G = g;
            B = b;
            A = a;
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

        /// <summary>
        /// The alpha component.
        /// </summary>
        [KRPCProperty]
        public float A { get; set; }
    }
}
