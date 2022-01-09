#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// ReSharper disable CheckNamespace
#endregion

namespace MicroElements.Text.Hashing
{
    using System.Security.Cryptography;
    using System.Text;

    internal static partial class HashingExtensions
    {
        /// <summary> Gets MD5 hash bytes for <see cref="content"/>. </summary>
        public static byte[] Md5HashBytes(this string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            using var cryptoServiceProvider = new MD5CryptoServiceProvider();
            byte[] hash = cryptoServiceProvider.ComputeHash(bytes);
            return hash;
        }

        /// <summary> Gets hash bytes as hex text. </summary>
        public static string AsHexText(this byte[] bytes)
        {
            var stringBuilder = new StringBuilder(bytes.Length * 2);
            foreach (var @byte in bytes)
                stringBuilder.Append(@byte.ToString("X2"));
            return stringBuilder.ToString();
        }

        /// <summary> Gets MD5 hash as hex text. </summary>
        public static string Md5HashAsHexText(this string content) =>
            content.Md5HashBytes().AsHexText();
    }
}
