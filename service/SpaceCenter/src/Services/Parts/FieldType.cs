using System;
using KRPC.Service.Attributes;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The type of a part module field. See <see cref="PartField.Type"/>.
    /// </summary>
    [Serializable]
    [KRPCEnum (Service = "SpaceCenter")]
    public enum FieldType
    {
        /// <summary>
        /// A boolean field. Access using <see cref="PartField.BoolValue"/>.
        /// </summary>
        Boolean,
        /// <summary>
        /// An integer field. Access using <see cref="PartField.IntValue"/>.
        /// </summary>
        Integer,
        /// <summary>
        /// A single precision floating point field. Access using <see cref="PartField.FloatValue"/>.
        /// </summary>
        Float,
        /// <summary>
        /// A double precision floating point field. Access using <see cref="PartField.DoubleValue"/>.
        /// </summary>
        Double,
        /// <summary>
        /// A string field. Access using <see cref="PartField.Value"/>.
        /// </summary>
        String,
        /// <summary>
        /// A field whose type is not one of the above. Its value can still be read as a
        /// string using <see cref="PartField.Value"/>.
        /// </summary>
        Unknown
    }
}
