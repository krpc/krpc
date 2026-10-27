using System;
using System.Diagnostics;
using Google.Protobuf;
using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// A stream.
    /// </summary>
    public abstract class Stream
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
                    var encoded = Encode (value);
                    Changed |= !StreamResult.Result.HasValue || !Equals (encoded, StreamResult.Result.EncodedValue);
                } else {
                    Changed |= value.HasError;
                }
                StreamResult.Result = value;
            }
        }

        /// <summary>
        /// Encode the value of a result, and store the encoding on it. Null for a null value.
        /// A stream compares encodings to find a change, as a collection changed in place is
        /// the same object, and 0.0 and -0.0 are equal values with different encodings.
        /// </summary>
        internal static ByteString Encode (ProcedureResult result)
        {
            if (ReferenceEquals (result.Value, null))
                return null;
            result.EncodedValue = Server.ProtocolBuffers.Encoder.Encode (result.Value, result.Spec);
            return result.EncodedValue;
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
