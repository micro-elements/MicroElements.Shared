// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using MicroElements.CodeContracts;

// ReSharper disable once CheckNamespace
namespace MicroElements.Formatting.StringFormatter
{
    /// <summary>
    /// Formatting extensions.
    /// </summary>
    internal static partial class StringFormatter
    {
        /// <summary>
        /// Default string formatting for most used types.
        /// </summary>
        /// <param name="value">Value to format.</param>
        /// <param name="nullPlaceholder">Optional null placeholder.</param>
        /// <returns>Formatted string.</returns>
#if NETSTANDARD2_1
        [return: NotNullIfNotNull("nullPlaceholder")]
#endif
        public static string? FormatValue(this object? value, string? nullPlaceholder = "null")
        {
            if (value == null)
                return nullPlaceholder;

            if (value is string stringValue)
                return stringValue;

            if (value is double doubleNumber)
                return doubleNumber.ToString(NumberFormatInfo.InvariantInfo);

            if (value is float floatNumber)
                return floatNumber.ToString(NumberFormatInfo.InvariantInfo);

            if (value is decimal decimalNumber)
                return decimalNumber.ToString(NumberFormatInfo.InvariantInfo);

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
        /// <param name="separator">The value that uses to separate items. DefaultValue = ', '.</param>
        /// <param name="nullPlaceholder">The value that renders if item is `null`. DefaultValue = `"null"`</param>
        /// <param name="startSymbol">Start symbol. DefaultValue = '('.</param>
        /// <param name="endSymbol">End symbol. DefaultValue = ')'.</param>
        /// <param name="formatValue">Func that formats object value to string representation. By default uses `FormatValue`</param>
        /// <param name="maxItems">The max number of items that will be formatted. By default not limited.</param>
        /// <param name="maxTextLength">TMax result text length. Used to limit result text size. DefaultValue=`1024`.</param>
        /// <param name="trimmedPlaceholder">The value that replaces trimmed part of sequence. DefaultValue = `"..."` </param>
        /// <returns>Formatted string.</returns>
        public static string FormatAsTuple(
            this IEnumerable? values,
            string separator = ", ",
            string nullPlaceholder = "null",
            string startSymbol = "(",
            string endSymbol = ")",
            Func<object, string?>? formatValue = null,
            int? maxItems = null,
            int maxTextLength = 1024,
            string trimmedPlaceholder = "...")
        {
            separator.AssertArgumentNotNull(nameof(separator));
            nullPlaceholder.AssertArgumentNotNull(nameof(nullPlaceholder));
            startSymbol.AssertArgumentNotNull(nameof(startSymbol));
            endSymbol.AssertArgumentNotNull(nameof(endSymbol));
            trimmedPlaceholder.AssertArgumentNotNull(nameof(trimmedPlaceholder));

            formatValue ??= value => value.FormatValue();

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(startSymbol);

            if (values != null)
            {
                int count = 1;
                foreach (var value in values)
                {
                    if (stringBuilder.Length > maxTextLength + trimmedPlaceholder.Length + endSymbol.Length)
                    {
                        stringBuilder.Length = maxTextLength - (trimmedPlaceholder.Length + endSymbol.Length);
                        stringBuilder.Append(trimmedPlaceholder).Append(separator);
                        break;
                    }
                    if (count > maxItems)
                    {
                        stringBuilder.Append(trimmedPlaceholder).Append(separator);
                        break;
                    }

                    string text = value != null ? formatValue(value) ?? nullPlaceholder : nullPlaceholder;
                    stringBuilder.Append(text).Append(separator);

                    count++;
                }

                if (stringBuilder.Length > separator.Length)
                    stringBuilder.Length -= separator.Length;
            }

            stringBuilder.Append(endSymbol);
            return stringBuilder.ToString();
        }
    }
}
