using System.Diagnostics.CodeAnalysis;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    /// <summary>
    /// Object representing a stream.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public class Stream<TReturnType>
    {
        readonly StreamManager streamManager;

        internal uint Id { get; private set; }

        internal Stream (Connection connection, Request request)
        {
            streamManager = connection.StreamManager;
            Id = streamManager.AddStream (request, typeof(TReturnType));
        }

        /// <summary>
        /// Get the most recent value of the stream.
        /// </summary>
        // FIXME: Change this to a property. This breaks compatibility.
        [SuppressMessage ("Gendarme.Rules.Design", "ConsiderConvertingMethodToPropertyRule")]
        public TReturnType Get ()
        {
            return (TReturnType)streamManager.GetValue (Id);
        }

        /// <summary>
        /// Remove the stream from the server.
        /// </summary>
        public void Remove ()
        {
            streamManager.RemoveStream (Id);
        }
    }
}
