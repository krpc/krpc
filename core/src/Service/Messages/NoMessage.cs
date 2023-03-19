using System.Diagnostics.CodeAnalysis;

namespace KRPC.Service.Messages
{
    /// <summary>
    /// Used to denote the absense of a message.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Design", "ConsiderUsingStaticTypeRule")]
    sealed class NoMessage
    {
        NoMessage ()
        {
        }
    }
}
