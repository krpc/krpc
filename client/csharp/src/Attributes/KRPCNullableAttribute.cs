using System;

namespace KRPC.Client.Attributes
{
    /// <summary>
    /// Attribute attached to a field of a structure type that can be null. A field whose type is
    /// Nullable&lt;T&gt; carries that at runtime and does not need this, where a reference-typed
    /// field is nullable in C# whether the service declares it so or not.
    /// </summary>
    [AttributeUsage (AttributeTargets.Property)]
    public sealed class KRPCNullableAttribute : Attribute
    {
    }
}
