using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;

namespace ReverseDSC
{
    internal static class PSInterop
    {
        internal static object? Unwrap(object? value)
        {
            return value is PSObject psObject ? psObject.BaseObject : value;
        }

        internal static Type? BaseTypeOf(object? value)
        {
            return Unwrap(value)?.GetType();
        }

        internal static string ToPSString(object? value)
        {
            return value is null ? string.Empty : LanguagePrimitives.ConvertTo<string>(value) ?? string.Empty;
        }

        internal static int CompareNames(string? left, string? right)
        {
            return LanguagePrimitives.Compare(left, right, true, CultureInfo.CurrentCulture);
        }

        internal static bool ContainsName(string[]? names, string name)
        {
            if (names is null)
            {
                return false;
            }

            foreach (string candidate in names)
            {
                if (string.Equals(candidate, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool AreEqual(string? left, string? right)
        {
            return string.Equals(left, right, StringComparison.InvariantCultureIgnoreCase);
        }

        internal static object?[]? ToObjectArray(object? value)
        {
            object? unwrapped = Unwrap(value);
            switch (unwrapped)
            {
                case null:
                    return null;
                case object?[] array:
                    return array;
                case string text:
                    return [text];
                case IEnumerable enumerable:
                    List<object?> items = [.. enumerable];

                    return [.. items];
                default:
                    return [unwrapped];
            }
        }

        internal static string Indent(int width)
        {
            return width <= 0 ? string.Empty : new string(' ', width);
        }
    }
}
