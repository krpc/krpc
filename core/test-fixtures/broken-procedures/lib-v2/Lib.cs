namespace BrokenProceduresLib
{
    /// <summary>
    /// The library installed at run time, without the method the fixture service uses.
    /// </summary>
    public static class Lib
    {
        /// <summary>
        /// A method kept from the previous version.
        /// </summary>
        public static int Kept ()
        {
            return 2;
        }
    }
}
