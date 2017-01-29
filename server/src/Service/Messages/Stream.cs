using System.Diagnostics.CodeAnalysis;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public class Stream : IMessage
    {
        public ulong Id { get; private set; }

        public Stream (ulong id)
        {
            Id = id;
        }
    }
}
