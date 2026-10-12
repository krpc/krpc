using System;
using System.Diagnostics;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// A stream.
    /// </summary>
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
        bool updated;

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
            rate = 0;
            delay = 0;
            timer = Stopwatch.StartNew ();
        }

        /// <summary>
        /// Send the current value in the next update.
        /// </summary>
        internal void Resend ()
        {
            Changed |= StreamResult.Result.HasValue || StreamResult.Result.HasError;
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
            // The first update is never rate limited, so a new stream sends its value at once
            if (updated && rate != 0 && timer.ElapsedMilliseconds < delay) {
                return;
            }
            updated = true;
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
        /// Set the result to an error, which ends the stream once it is sent.
        /// </summary>
        internal void SetError (System.Exception exn)
        {
            var result = StreamResult.Result;
            result.Reset ();
            result.Error = Services.Instance.HandleException (exn);
            Changed = true;
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
