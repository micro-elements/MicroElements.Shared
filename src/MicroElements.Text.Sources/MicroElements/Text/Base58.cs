#region License
// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#endregion
#region Supressions
#pragma warning disable
// ReSharper disable CheckNamespace
#endregion

namespace MicroElements.Text.Base58
{
    using System;
    using System.Buffers;
    using System.Numerics;
    using MicroElements.Text.Hashing;

    /// <summary id="Base58">
    /// Base58 encoding provides fast encoding for small amount of data for example hashes.
    /// 
    /// Benefits over Base64 encoding:
    /// - Human readable because excludes similar characters 0OIl that looks the same in some fonts and could be used to create visually identical looking data.
    /// - Does not have line-breaks and special symbols so can be typed easy.
    /// - Double-clicking selects the whole string as one word if it's all alphanumeric.
    /// </summary>
    internal static partial class Base58
    {
        /// <summary> Bitcoin base58 alphabet. </summary>
        public static string BitcoinAlphabet => "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        /// Embeddable Base58 algorithm modified by @petriashev to be small and fast as possible.
        /// </summary>
        /// <param name="inputBytes">Input bytes to encode.</param>
        /// <param name="alphabet">Optional base58 alphabet.</param>
        /// <returns>Base58 encoded string.</returns>
        public static string Encode(ReadOnlySpan<byte> inputBytes, string? alphabet = null)
        {
            alphabet ??= BitcoinAlphabet;
            int encodingBase = alphabet.Length;
            int resultMaxLength = (inputBytes.Length * 138 / 100) + 1;
            var outputChars = ArrayPool<char>.Shared.Rent(resultMaxLength);
            int outputIndex = outputChars.Length;

            // Decode byte[] to BigInteger
            var bigInt = new BigInteger(inputBytes, isUnsigned: true, isBigEndian: true);

            // Encode BigInteger to Base58 string
            while (bigInt > 0 && outputIndex > 0)
            {
                bigInt = BigInteger.DivRem(bigInt, encodingBase, out var remainder);
                outputChars[--outputIndex] = alphabet[(int)remainder];
            }

            // Append ZeroChar for each leading 0 byte
            for (int i = 0; i < inputBytes.Length && inputBytes[i] == 0 && outputIndex > 0; i++)
                outputChars[--outputIndex] = alphabet[0];

            var encode = new string(outputChars[outputIndex..]);
            ArrayPool<char>.Shared.Return(outputChars);
            return encode;
        }
    }

    internal static partial class Base58Extensions
    {
        /// <summary>
        /// Encodes input bytes in Base58. 
        /// </summary>
        /// <param name="inputBytes">Input bytes.</param>
        /// <param name="alphabet">Optional base58 alphabet.</param>
        /// <returns>Base58 encoded string.</returns>
        public static string EncodeBase58(this byte[] inputBytes, string? alphabet = null) =>
            Base58.Encode(inputBytes, alphabet);

        /// <summary>
        /// Gets Md5 hash encoded with base58.
        /// </summary>
        /// <param name="source">Source text.</param>
        /// <param name="alphabet">Optional base58 alphabet.</param>
        /// <returns>Base58 encoded hash string.</returns>
        public static string Md5HashInBase58(this string source, string? alphabet = null) =>
            source.Md5HashBytes().EncodeBase58(alphabet);
    }
}
