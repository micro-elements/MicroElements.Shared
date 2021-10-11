// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using MicroElements.CodeContracts;

namespace MicroElements.Formatting
{
    /// <summary>
    /// Formatting extensions.
    /// </summary>
    internal static partial class StringFormatter
    {
        /// <summary>
        /// Invariant format info. Uses '.' as decimal separator for floating point numbers.
        /// </summary>
        private static readonly NumberFormatInfo DefaultNumberFormatInfo = NumberFormatInfo.ReadOnly(new NumberFormatInfo { NumberDecimalSeparator = "." });

        /// <summary>
        /// Default string formatting for most used types.
        /// </summary>
        /// <param name="value">Value to format.</param>
        /// <param name="nullPlaceholder">Optional null placeholder.</param>
        /// <returns>Formatted string.</returns>
        [return: NotNullIfNotNull("nullPlaceholder")]
        public static string? FormatValue(this object? value, string? nullPlaceholder = "null")
        {
            if (value == null)
                return nullPlaceholder;

            if (value is string stringValue)
                return stringValue;

            if (value is double doubleNumber)
                return doubleNumber.ToString(DefaultNumberFormatInfo);

            if (value is float floatNumber)
                return floatNumber.ToString(DefaultNumberFormatInfo);

            if (value is decimal decimalNumber)
                return decimalNumber.ToString(DefaultNumberFormatInfo);

            if (value is DateTime dateTime)
                return dateTime == dateTime.Date ? $"{dateTime:yyyy-MM-dd}" : $"{dateTime:yyyy-MM-ddTHH:mm:ss}";

            if (value is DateTimeOffset dateTimeOffset)
                return $"{dateTimeOffset:yyyy-MM-ddTHH:mm:ss.fffK}";

            string typeFullName = value.GetType().FullName;

            if (typeFullName == "NodaTime.LocalDate" && value is IFormattable localDate)
                return localDate.ToString("yyyy-MM-dd", null);

            if (typeFullName == "NodaTime.LocalDateTime" && value is IFormattable localDateTime)
                return localDateTime.ToString("yyyy-MM-ddTHH:mm:ss", null);

            if (value is ICollection collection)
                return collection.FormatAsTuple(startSymbol: "[", endSymbol: "]");

            if (value is ValueTuple<string, object?> nameValueTuple)
                return $"({nameValueTuple.Item1}: {FormatValue(nameValueTuple.Item2)})";

            if (value is KeyValuePair<string, object?> keyValuePair)
                return $"({keyValuePair.Key}: {FormatValue(keyValuePair.Value)})";

            return $"{value}";
        }

        /// <summary>
        /// Formats enumeration of value as tuple: (value1, value2, ...).
        /// </summary>
        /// <param name="values">Values enumeration.</param>
        /// <param name="fieldSeparator">Optional field separator.</param>
        /// <param name="nullPlaceholder">Optional null placeholder.</param>
        /// <param name="startSymbol">Start symbol. DefaultValue='('.</param>
        /// <param name="endSymbol">End symbol. DefaultValue=')'.</param>
        /// <param name="formatValue">Func to format object value to string representation.</param>
        /// <param name="maxItems">Optional max items to render.</param>
        /// <param name="maxTextLength">Limits max text length.</param>
        /// <returns>Formatted string.</returns>
        public static string FormatAsTuple(
            this IEnumerable? values,
            string fieldSeparator = ", ",
            string nullPlaceholder = "null",
            string startSymbol = "(",
            string endSymbol = ")",
            Func<object, string?>? formatValue = null,
            int? maxItems = null,
            int maxTextLength = 1028)
        {
            fieldSeparator.AssertArgumentNotNull(nameof(fieldSeparator));
            nullPlaceholder.AssertArgumentNotNull(nameof(nullPlaceholder));
            startSymbol.AssertArgumentNotNull(nameof(startSymbol));
            endSymbol.AssertArgumentNotNull(nameof(endSymbol));

            formatValue ??= value => value.FormatValue();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(startSymbol);

            if (values != null)
            {
                int count = 1;
                foreach (var value in values)
                {
                    if (stringBuilder.Length > maxTextLength || (maxItems.HasValue && count > maxItems.Value))
                        break;

                    string text = value != null ? formatValue(value) ?? nullPlaceholder : nullPlaceholder;
                    stringBuilder.Append($"{text}{fieldSeparator}");

                    count++;
                }

                if (stringBuilder.Length > maxTextLength || (maxItems.HasValue && count > maxItems.Value))
                    stringBuilder.Append($"...{fieldSeparator}");

                if (stringBuilder.Length > fieldSeparator.Length)
                    stringBuilder.Length -= fieldSeparator.Length;
            }

            stringBuilder.Append(endSymbol);
            return stringBuilder.ToString();
        }
    }
}
