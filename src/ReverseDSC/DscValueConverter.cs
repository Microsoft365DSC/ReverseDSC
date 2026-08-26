using System;
using System.Collections;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Management.Infrastructure;

namespace ReverseDSC
{
    internal static class DscValueConverter
    {
        internal const int DefaultIndent = 12;

        private const string EmptyArray = "@()";

        private static readonly Regex IntegerArrayTypeName =
            new(@"Int.*\[\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static string ConvertValue(ModuleState state, object value, string parameterName, int indent, bool noEscape, bool allowVariables)
        {
            StringBuilder builder = new();
            AppendValue(builder, state, value, parameterName, indent, noEscape, allowVariables);
            return builder.ToString();
        }

        internal static void AppendValue(StringBuilder builder, ModuleState state, object value, string parameterName, int indent, bool noEscape, bool allowVariables)
        {
            if (DscClassTypeInspector.IsDscClassInstanceArray(state, value))
            {
                DscClassInstanceConverter.AppendInstanceArray(builder, state, PSInterop.ToObjectArray(value), indent, noEscape, allowVariables);
                return;
            }

            if (DscClassTypeInspector.IsDscClassInstance(state, value))
            {
                DscClassInstanceConverter.AppendInstance(builder, state, value, indent, noEscape, allowVariables);
                return;
            }

            Type valueType = PSInterop.BaseTypeOf(value)!;
            switch (valueType.Name)
            {
                case "String":
                case "Guid":
                case "TimeSpan":
                case "DateTime":
                    builder.Append(ConvertString(PSInterop.ToPSString(value), noEscape, allowVariables));
                    return;
                case "Boolean":
                    builder.Append(ConvertBoolean((bool)PSInterop.Unwrap(value)!));
                    return;
                case "PSCredential":
                    builder.Append(ConvertCredential((PSCredential)PSInterop.Unwrap(value)!, parameterName));
                    return;
                case "Hashtable":
                    builder.Append(ConvertHashtable((IDictionary)PSInterop.Unwrap(value)!));
                    return;
                case "String[]":
                case "ArrayList":
                case "List`1":
                    builder.Append(ConvertStringArray(PSInterop.ToObjectArray(value), noEscape, allowVariables));
                    return;
            }

            if (IntegerArrayTypeName.IsMatch(valueType.Name))
            {
                builder.Append(ConvertIntegerArray(PSInterop.ToObjectArray(value)));
                return;
            }

            switch (valueType.Name)
            {
                case "CimInstance":
                    DscCimInstanceConverter.AppendInstance(builder, state, (CimInstance)PSInterop.Unwrap(value)!, indent, noEscape, allowVariables);
                    return;
                case "CimInstance[]":
                    DscCimInstanceConverter.AppendInstanceArray(builder, state, (CimInstance[])PSInterop.Unwrap(value)!, indent, noEscape, allowVariables);
                    return;
                case "Object[]":
                    builder.Append(ConvertObjectArray(PSInterop.ToObjectArray(value), noEscape, allowVariables));
                    return;
            }

            if (valueType.IsEnum)
            {
                builder.Append('"').Append(PSInterop.ToPSString(value)).Append('"');
                return;
            }

            builder.Append(PSInterop.ToPSString(value));
        }

        internal static string ConvertString(string? value, bool noEscape, bool allowVariables)
        {
            if (noEscape)
            {
                return value ?? string.Empty;
            }

            return string.Concat("\"", DscStringEscaper.Escape(value, allowVariables), "\"");
        }

        internal static string ConvertBoolean(bool value)
        {
            return value ? "$True" : "$False";
        }

        internal static string ConvertCredential(PSCredential? value, string parameterName)
        {
            if (value is null)
            {
                return "Get-Credential -Message " + parameterName;
            }

            string userName = value.UserName;
            if (userName.IndexOf('@') >= 0 && userName.IndexOf('\\') < 0)
            {
                return FormatCredentialVariableName(userName.Split('@')[0]);
            }

            string[] domainParts = userName.Split('\\');
            return FormatCredentialVariableName(domainParts[domainParts.Length - 1]);
        }

        internal static string FormatCredentialVariableName(string userName)
        {
            return "$Creds" + userName.Replace("-", "_").Replace(".", "_").Replace(" ", string.Empty).Replace("@", string.Empty);
        }

        internal static string ConvertHashtable(IDictionary value)
        {
            StringBuilder result = new("@{");
            foreach (object key in value.Keys)
            {
                result.Append(PSInterop.ToPSString(key))
                    .Append(" = \"")
                    .Append(PSInterop.ToPSString(value[key]))
                    .Append("\"; ");
            }

            return result.Append('}').ToString();
        }

        internal static string ConvertStringArray(object?[]? value, bool noEscape, bool allowVariables)
        {
            if (IsEmptyOrSingleNull(value))
            {
                return EmptyArray;
            }

            StringBuilder result = new("@(");
            bool first = true;
            foreach (object? item in value!)
            {
                if (item is null)
                {
                    continue;
                }

                if (!first)
                {
                    result.Append(',');
                }

                string text = PSInterop.ToPSString(item);
                result.Append('"').Append(noEscape ? text : DscStringEscaper.Escape(text, allowVariables)).Append('"');
                first = false;
            }

            return result.Append(')').ToString();
        }

        internal static string ConvertIntegerArray(object?[]? value)
        {
            if (IsEmptyOrSingleNull(value))
            {
                return EmptyArray;
            }

            StringBuilder result = new("@(");
            for (int index = 0; index < value!.Length; index++)
            {
                if (index > 0)
                {
                    result.Append(',');
                }

                result.Append(PSInterop.ToPSString(value[index]));
            }

            return result.Append(')').ToString();
        }

        internal static string ConvertObjectArray(object?[]? value, bool noEscape, bool allowVariables)
        {
            if (IsEmptyOrSingleNull(value))
            {
                return EmptyArray;
            }

            string firstTypeName = PSInterop.BaseTypeOf(value![0])!.Name;
            if (string.Equals(firstTypeName, "String", StringComparison.Ordinal))
            {
                return ConvertStringObjectArray(value, noEscape, allowVariables);
            }

            if (string.Equals(firstTypeName, "Hashtable", StringComparison.Ordinal))
            {
                return ConvertHashtableObjectArray(value);
            }

            StringBuilder result = new("@(");
            foreach (object? item in value)
            {
                result.Append(PSInterop.ToPSString(item));
            }

            return result.Append(')').ToString();
        }

        private static string ConvertStringObjectArray(object?[] value, bool noEscape, bool allowVariables)
        {
            if (noEscape)
            {
                StringBuilder joined = new();
                foreach (object? item in value)
                {
                    joined.Append(PSInterop.ToPSString(item));
                }

                if (joined.Length > 0 && joined[joined.Length - 1] == ',')
                {
                    joined.Length -= 1;
                }

                return string.Concat("@(", joined.ToString(), ")");
            }

            StringBuilder result = new("@(");
            for (int index = 0; index < value.Length; index++)
            {
                if (index > 0)
                {
                    result.Append(',');
                }

                result.Append('"')
                    .Append(DscStringEscaper.Escape(PSInterop.ToPSString(value[index]), allowVariables))
                    .Append('"');
            }

            return result.Append(')').ToString();
        }

        private static string ConvertHashtableObjectArray(object?[] value)
        {
            StringBuilder result = new("@(");
            foreach (object? item in value)
            {
                IDictionary hashtable = (IDictionary)PSInterop.Unwrap(item)!;
                result.Append("@{");
                bool first = true;
                foreach (DictionaryEntry pair in hashtable)
                {
                    if (!first)
                    {
                        result.Append("; ");
                    }

                    AppendHashtablePair(result, pair);
                    first = false;
                }

                result.Append('}');
            }

            return result.Append(')').ToString();
        }

        private static void AppendHashtablePair(StringBuilder result, DictionaryEntry pair)
        {
            result.Append(PSInterop.ToPSString(pair.Key)).Append('=');
            object? pairValue = PSInterop.Unwrap(pair.Value);
            if (pairValue is Array items)
            {
                result.Append("@('");
                for (int index = 0; index < items.Length; index++)
                {
                    if (index > 0)
                    {
                        result.Append("', '");
                    }

                    result.Append(PSInterop.ToPSString(items.GetValue(index)));
                }

                result.Append("')");
                return;
            }

            if (pairValue is null)
            {
                result.Append("$null");
                return;
            }

            result.Append('\'').Append(PSInterop.ToPSString(pairValue)).Append('\'');
        }

        internal static bool IsEmptyOrSingleNull(object?[]? value)
        {
            return value is null || value.Length == 0 || (value.Length == 1 && value[0] is null);
        }
    }
}
