using KRPC.Service.Attributes;

namespace KRPC.KSA.FlightSoftware
{
    /// <summary>
    /// An error or warning from building a program. See <see cref="Computer.Build"/>.
    /// </summary>
    [KRPCStruct (Service = "KFS")]
    public struct Diagnostic
    {
        /// <summary>
        /// Whether it is an error. An error stops the program from being built.
        /// </summary>
        [KRPCProperty]
        public bool Error { get; set; }

        /// <summary>
        /// The diagnostic's code, such as <c>KC-E016</c>.
        /// </summary>
        [KRPCProperty]
        public string Code { get; set; }

        /// <summary>
        /// The source file.
        /// </summary>
        [KRPCProperty]
        public string File { get; set; }

        /// <summary>
        /// The line in the source file, from 1. Zero for the whole file.
        /// </summary>
        [KRPCProperty]
        public int Line { get; set; }

        /// <summary>
        /// The column in the line, from 1.
        /// </summary>
        [KRPCProperty]
        public int Column { get; set; }

        /// <summary>
        /// The message.
        /// </summary>
        [KRPCProperty]
        public string Message { get; set; }

        /// <summary>
        /// A hint at the fix, or <c>null</c> if there is none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public string Hint { get; set; }
    }

    /// <summary>
    /// The error that halted a program, and where it happened. See <see cref="Computer.Trap"/>.
    /// </summary>
    [KRPCStruct (Service = "KFS")]
    public struct Trap
    {
        /// <summary>
        /// The error.
        /// </summary>
        [KRPCProperty]
        public TrapCode Code { get; set; }

        /// <summary>
        /// The function that was running.
        /// </summary>
        [KRPCProperty]
        public string Function { get; set; }

        /// <summary>
        /// The source file, or <c>null</c> if it is not known.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public string File { get; set; }

        /// <summary>
        /// The line in the source file, or zero if it is not known.
        /// </summary>
        [KRPCProperty]
        public int Line { get; set; }
    }
}
