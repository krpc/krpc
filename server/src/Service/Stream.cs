using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Messages;
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
        /// Whether the stream has been started.
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// The value of the stream.
        /// </summary>
        public virtual ProcedureResult Result {
            get { return StreamResult.Result; }
            set {
                if (ReferenceEquals(value, null))
                    throw new ArgumentNullException ("Result");
                if (value.HasValue)
                    if (!ReferenceEquals(value.Value, null))
                        Changed |= !value.Value.Equals (StreamResult.Result.Value);
                    else
                        Changed |= (ReferenceEquals(value.Value, null) ^ ReferenceEquals(StreamResult.Result.Value, null));
                else
                    Changed |= value.HasError;
                StreamResult.Result = value;
            }
        }

        /// <summary>
        /// Construct a stream.
        /// </summary>
        protected Stream ()
        {
            StreamResult = new StreamResult ();
            Started = false;
        }

        /// <summary>
        /// Start the stream.
        /// </summary>
        public void Start()
        {
            Started = true;
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
