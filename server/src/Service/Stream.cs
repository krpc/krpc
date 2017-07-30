using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Server;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// A stream.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public abstract class Stream : Equatable<Stream>
    {
        internal StreamResult StreamResult { get; private set; }

        internal ulong Id {
            get { return StreamResult.Id; }
            set { StreamResult.Id = value; }
        }

        /// <summary>
        /// The value of the stream.
        /// </summary>
        public virtual ProcedureResult Result {
            get { return StreamResult.Result; }
            set {
                if (value != null)
                    Changed |= !value.Equals (StreamResult.Result);
                else
                    Changed |= (value == null ^ StreamResult.Result == null);
                StreamResult.Result = value;
            }
        }

        /// <summary>
        /// Construct a stream.
        /// </summary>
        protected Stream ()
        {
            StreamResult = new StreamResult ();
        }

        /// <summary>
        /// Called when the stream value should be updated.
        /// </summary>
        public virtual void Update() {
        }

        /// <summary>
        /// Called when the stream value has been sent to the client.
        /// </summary>
        public virtual void Sent() {
            Changed = false;
        }

        /// <summary>
        /// Returns whether the stream value has changed.
        /// </summary>
        public bool Changed { get; set; }
    }
}
