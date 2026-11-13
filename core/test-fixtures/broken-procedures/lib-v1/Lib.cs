namespace BrokenProceduresLib
{
    /// <summary>
    /// The library the fixture service is built against.
    /// </summary>
    public static class Lib
    {
        /// <summary>
        /// A method the next version of the library removes.
        /// </summary>
        public static int Removed ()
        {
            return 1;
        }

        /// <summary>
        /// A method the next version of the library keeps.
        /// </summary>
        public static int Kept ()
        {
            return 2;
        }
    }
}
