#region License

// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#endregion
#region Supressions

#pragma warning disable
// ReSharper disable CheckNamespace

#endregion

#if ME_EXTERNALINIT_ENABLE || ((NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER) && !NET5_0_OR_GREATER && !ME_EXTERNALINIT_DISABLE)

namespace System.Runtime.CompilerServices
{
    using ComponentModel;

    /// <summary id="IsExternalInit">
    /// Allows to use C#9 records and init only setters.
    /// Class IsExternalInit is reserved for compiler needs and should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
