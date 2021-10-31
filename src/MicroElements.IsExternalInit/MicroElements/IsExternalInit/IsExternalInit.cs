#if (NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER) && !NET5_0_OR_GREATER && !ISEXTERNALINIT_DISABLE

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    using ComponentModel;
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
