using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace ReverseDSC
{
    internal static class DscBlockGenerator
    {
        private const string MetadataKeyPrefix = "_metadata_";
        private const string PropertyIndent = "            ";
        private const int MinimumNameLength = 20;

        private static readonly WildcardPattern MetadataKeyPattern =
            WildcardPattern.Get(MetadataKeyPrefix + "*", WildcardOptions.IgnoreCase);

        internal static string GenerateBlock(ModuleState state, Hashtable parameters, string[]? noEscape, bool allowVariablesInStrings)
        {
            List<KeyValuePair<string, object>> retained = GetSortedValuedParameters(parameters, out int maximumNameLength);

            StringBuilder block = new();
            foreach (KeyValuePair<string, object> parameter in retained)
            {
                block.Append(PropertyIndent).Append(parameter.Key.PadRight(maximumNameLength)).Append(" = ");
                DscValueConverter.AppendValue(
                    block,
                    state,
                    parameter.Value,
                    parameter.Key,
                    DscValueConverter.DefaultIndent,
                    PSInterop.ContainsName(noEscape, parameter.Key),
                    allowVariablesInStrings);
                block.Append(';');

                string metadataKey = MetadataKeyPrefix + parameter.Key;
                if (parameters.ContainsKey(metadataKey))
                {
                    block.Append(' ').Append(PSInterop.ToPSString(parameters[metadataKey]));
                }

                block.Append("\r\n");
            }

            return block.ToString();
        }

        internal static string GenerateDependsOnBlock(object?[] dependsOnItems)
        {
            StringBuilder block = new("@(");
            for (int index = 0; index < dependsOnItems.Length; index++)
            {
                if (index > 0)
                {
                    block.Append(',');
                }

                block.Append('"').Append(PSInterop.ToPSString(dependsOnItems[index])).Append('"');
            }

            return block.Append(");").ToString();
        }

        private static List<KeyValuePair<string, object>> GetSortedValuedParameters(Hashtable parameters, out int maximumNameLength)
        {
            List<KeyValuePair<string, object?>> candidates = [];
            foreach (DictionaryEntry entry in parameters)
            {
                string key = PSInterop.ToPSString(entry.Key);
                if (MetadataKeyPattern.IsMatch(key))
                {
                    continue;
                }

                candidates.Add(new KeyValuePair<string, object?>(key, entry.Value));
            }

            maximumNameLength = MinimumNameLength;
            List<KeyValuePair<string, object>> retained = [];
            foreach (KeyValuePair<string, object?> candidate in candidates.OrderBy(entry => entry.Key, ParameterNameComparer.Instance))
            {
                if (candidate.Value is null)
                {
                    continue;
                }

                retained.Add(new KeyValuePair<string, object>(candidate.Key, candidate.Value));
                if (candidate.Key.Length > maximumNameLength)
                {
                    maximumNameLength = candidate.Key.Length;
                }
            }

            return retained;
        }

        private sealed class ParameterNameComparer : IComparer<string>
        {
            internal static readonly ParameterNameComparer Instance = new();

            public int Compare(string? x, string? y)
            {
                return PSInterop.CompareNames(x, y);
            }
        }
    }
}
