#pragma warning disable 1591

using System;
using System.Reflection;

namespace KRPC.InfernalRobotics.IRWrapper
{
    public abstract class IRWrapper
    {
        protected bool isWrapped;
        internal Type IRControllerType { get; set; }
        internal Type IRControlGroupType { get; set; }
        internal Type IRServoType { get; set; }
        internal Type IRMotorType { get; set; }
        internal object ActualController { get; set; }
        internal IRAPI IRController { get; set; }
        internal bool AssemblyExists { get { return (IRControllerType != null); } }
        internal bool InstanceExists { get { return (IRController != null); } }
        internal bool APIReady { get { return isWrapped && IRController.Ready; } }

        static IRWrapper instance;

        public static IRWrapper Instance { get { return instance; } }

        public static void InitWrapper () {
            if (instance == null)
            {
                // Try loading IR Next
                var irNext = new IRNextWrapper();
                irNext.InternalInitWrapper();
                if (irNext.AssemblyExists) {
                    instance = irNext;
                } else {
                    // Try loading original IR
                    var irOrig = new IROrigWrapper();
                    irOrig.InternalInitWrapper();
                    if (irOrig.AssemblyExists)
                        instance = irOrig;
                }
            }
        }

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        [System.Diagnostics.Conditional ("DEBUG")]
        internal void LogFormatted_DebugOnly (string message, params object[] strParams)
        {
            LogFormatted (message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        internal void LogFormatted (string message, params object[] strParams)
        {
            var assemblyName = Assembly.GetExecutingAssembly ().GetName ().Name;
            var declaringType = MethodBase.GetCurrentMethod ().DeclaringType;
            message = string.Format (message, strParams);

            string strMessageLine = declaringType != null ?
                string.Format ("{0},{2}-{3},{1}", DateTime.Now, message, assemblyName, declaringType.Name) :
                      string.Format ("{0},{2}-NO-DECLARE,{1}", DateTime.Now, message, assemblyName);

            UnityEngine.Debug.Log (strMessageLine);
        }

        #endregion Logging Stuff
    }
}
