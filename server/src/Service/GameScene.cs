using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Service
{
    /// <summary>
    /// KSP game scenes
    /// </summary>
    [Flags]
    [Serializable]
    [SuppressMessage ("Gendarme.Rules.Design", "FlagsShouldNotDefineAZeroValueRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "UsePluralNameInEnumFlagsRule")]
    public enum GameScene
    {
        /// <summary>
        /// None of the scenes
        /// </summary>
        None = 0,

        /// <summary>
        /// The space center overview
        /// </summary>
        SpaceCenter = 1 << 0,

        /// <summary>
        /// When piloting a vessel
        /// </summary>
        Flight = 1 << 1,

        /// <summary>
        /// The tracking station
        /// </summary>
        TrackingStation = 1 << 2,

        /// <summary>
        /// The VAB editor
        /// </summary>
        EditorVAB = 1 << 3,

        /// <summary>
        /// The SPH editor
        /// </summary>
        EditorSPH = 1 << 4,

        /// <summary>
        /// All game scenes
        /// </summary>
        All = ~0,

        /// <summary>
        /// The VAB or SPH editors
        /// </summary>
        Editor = EditorSPH | EditorVAB
    }
}
