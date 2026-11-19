using KRPC.Service.Attributes;

namespace KRPC.KSA.FlightSoftware
{
    /// <summary>
    /// The state of a flight computer. See <see cref="Computer.State"/>.
    /// </summary>
    [KRPCEnum (Service = "KFS")]
    public enum ComputerState
    {
        /// <summary>
        /// No program is loaded.
        /// </summary>
        NoSoftware,
        /// <summary>
        /// A program is loaded, and not running.
        /// </summary>
        Standby,
        /// <summary>
        /// The program is running its initialization.
        /// </summary>
        Boot,
        /// <summary>
        /// The program is running.
        /// </summary>
        Running,
        /// <summary>
        /// The program is running, with no task to run.
        /// </summary>
        Idle,
        /// <summary>
        /// The program has stopped, after a trap, a halt or the kill switch.
        /// </summary>
        Halted
    }

    /// <summary>
    /// The control a program has of a vehicle. See <see cref="Computer.Mode"/>.
    /// </summary>
    [KRPCEnum (Service = "KFS")]
    public enum ControlMode
    {
        /// <summary>
        /// The game's flight computer flies the vehicle, and the program steers it.
        /// </summary>
        Pilot,
        /// <summary>
        /// The program commands the thrusters, engines and gimbals itself. A program claims it
        /// with <c>#pragma kfs claim ALLOCATION</c>.
        /// </summary>
        FullControl
    }

    /// <summary>
    /// The error that halted a program. See <see cref="Trap.Code"/>.
    /// </summary>
    [KRPCEnum (Service = "KFS")]
    public enum TrapCode
    {
        /// <summary>
        /// An instruction the processor does not have.
        /// </summary>
        IllegalOpcode = 1,
        /// <summary>
        /// The operand stack overflowed.
        /// </summary>
        OperandOverflow,
        /// <summary>
        /// The operand stack underflowed.
        /// </summary>
        OperandUnderflow,
        /// <summary>
        /// The return stack overflowed.
        /// </summary>
        ReturnOverflow,
        /// <summary>
        /// The initialization ran past its budget.
        /// </summary>
        InitOverrun,
        /// <summary>
        /// The stack overflowed.
        /// </summary>
        StackOverflow,
        /// <summary>
        /// A read or write through a null pointer.
        /// </summary>
        NullAccess,
        /// <summary>
        /// A read or write outside memory.
        /// </summary>
        BadAddress,
        /// <summary>
        /// A write to read-only memory.
        /// </summary>
        ReadOnly,
        /// <summary>
        /// A misaligned read or write.
        /// </summary>
        Misaligned,
        /// <summary>
        /// A division by zero.
        /// </summary>
        DivisionByZero,
        /// <summary>
        /// An array index out of bounds.
        /// </summary>
        Bounds,
        /// <summary>
        /// A trap raised by the program.
        /// </summary>
        User,
        /// <summary>
        /// A jump to an invalid address.
        /// </summary>
        BadJump,
        /// <summary>
        /// A call to a service that does not exist.
        /// </summary>
        BadSyscall,
        /// <summary>
        /// A task yielded where it may not.
        /// </summary>
        BadYield,
        /// <summary>
        /// A memory error, such as a radiation upset.
        /// </summary>
        MemoryError
    }
}
