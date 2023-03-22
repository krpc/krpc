using System;
using System.Diagnostics;
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

        float rate;
        float delay;
        Stopwatch timer;

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
                if (value.HasValue) {
                    if (!StreamResult.Result.HasValue)
                        Changed = true;
                    else if (!ReferenceEquals(value.Value, null))
                        Changed |= !ValueUtils.Equal(value.Value, StreamResult.Result.Value);
                    else
                        Changed |= (ReferenceEquals(value.Value, null) ^ ReferenceEquals(StreamResult.Result.Value, null));
                } else {
                    Changed |= value.HasError;
                }
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
            rate = 0;
            delay = 0;
            timer = new Stopwatch();
        }

        /// <summary>
        /// Start the stream.
        /// </summary>
        public void Start()
        {
            Started = true;
            Changed |= StreamResult.Result.HasValue || StreamResult.Result.HasError;
            timer.Start();
        }

        /// <summary>
        /// The update rate of the stream in Hz.
        /// </summary>
        public float Rate {
          get { return rate; }
          set {
              rate = value;
              delay = rate == 0 ? 0 : 1000.0f / rate;
          }
        }

        /// <summary>
        /// Called when the stream value should be updated.
        /// Rate limiting is applied by this method.
        /// </summary>
        public void Update() {
            if (rate != 0 && timer.ElapsedMilliseconds < delay) {
                return;
            }
            UpdateInternal();
            if (rate != 0) {
                timer.Reset();
                timer.Start();
            }
        }

        /// <summary>
        /// Implements the actual stream update.
        /// </summary>
        public virtual void UpdateInternal() {
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
