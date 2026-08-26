using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace ReverseDSC
{
    internal static class ConfigurationDataStore
    {
        private const string NonNodeData = "NonNodeData";
        private const string EntriesKey = "Entries";
        private const string ValueKey = "Value";
        private const string DescriptionKey = "Description";
        private const string EntryIndent = "            ";

        internal static void AddEntry(ModuleState state, string node, string key, object value, string? description)
        {
            if (state.ConfigurationData[node] is null)
            {
                Hashtable created = ModuleState.NewHashtable();
                created.Add(EntriesKey, new OrderedDictionary(StringComparer.OrdinalIgnoreCase));
                state.ConfigurationData.Add(node, created);
            }

            Hashtable entry = ModuleState.NewHashtable();
            entry[ValueKey] = value;
            entry[DescriptionKey] = description;
            GetEntries(state.ConfigurationData[node])[key] = entry;
        }

        internal static Hashtable? GetEntry(ModuleState state, string? node, string key)
        {
            if (string.IsNullOrEmpty(node))
            {
                foreach (object nodeName in state.ConfigurationData.Keys)
                {
                    OrderedDictionary entries = GetEntries(state.ConfigurationData[nodeName]);
                    if (entries.Contains(key))
                    {
                        return (Hashtable)entries[key];
                    }
                }

                return null;
            }

            if (!state.ConfigurationData.ContainsKey(node!))
            {
                return null;
            }

            OrderedDictionary nodeEntries = GetEntries(state.ConfigurationData[node!]);
            return nodeEntries.Contains(key) ? (Hashtable)nodeEntries[key] : null;
        }

        internal static string GetContent(ModuleState state, Action<string>? warn)
        {
            StringBuilder content = new();
            content.Append("@{\r\n");
            content.Append("    AllNodes = @(\r\n");

            foreach (object node in state.ConfigurationData.Keys)
            {
                if (PSInterop.AreEqual(PSInterop.ToPSString(node), NonNodeData))
                {
                    continue;
                }

                AppendNode(content, state, node);
            }

            TrimTrailingComma(content);
            content.Append("    )\r\n");
            content.Append("    NonNodeData = @(\r\n");

            foreach (object node in state.ConfigurationData.Keys)
            {
                if (!PSInterop.AreEqual(PSInterop.ToPSString(node), NonNodeData))
                {
                    continue;
                }

                AppendNonNodeData(content, state, node, warn);
            }

            content.Append("    )\r\n");
            content.Append('}');
            return content.ToString();
        }

        internal static void WriteDocument(ModuleState state, string path, Action<string>? warn)
        {
            File.WriteAllText(path, GetContent(state, warn) + "\r\n", new UTF8Encoding(false));
        }

        internal static string ConvertToConfigurationDataString(object? value)
        {
            StringBuilder content = new();
            switch (PSInterop.BaseTypeOf(value)?.FullName)
            {
                case "System.String":
                    content.Append('"').Append(PSInterop.ToPSString(value)).Append("\";\r\n");
                    break;
                case "System.Object[]":
                    content.Append(EntryIndent).Append("@(\r\n");
                    foreach (object? entry in (object?[])PSInterop.Unwrap(value)!)
                    {
                        content.Append(ConvertToConfigurationDataString(entry));
                    }

                    TrimTrailingComma(content);
                    content.Append(EntryIndent).Append(")\r\n");
                    break;
                case "System.Collections.Hashtable":
                    content.Append(EntryIndent).Append("@{\r\n");
                    Hashtable hashtable = (Hashtable)PSInterop.Unwrap(value)!;
                    foreach (object key in hashtable.Keys)
                    {
                        content.Append("                ").Append(PSInterop.ToPSString(key)).Append(" = ");
                        content.Append(ConvertToConfigurationDataString(hashtable[key]));
                    }

                    content.Append(EntryIndent).Append("},\r\n");
                    break;
            }

            return content.ToString();
        }

        private static void AppendNode(StringBuilder content, ModuleState state, object node)
        {
            content.Append("        @{\r\n");
            content.Append("            NodeName                    = \"").Append(PSInterop.ToPSString(node)).Append("\"\r\n");
            content.Append("            PSDscAllowPlainTextPassword = $true;\r\n");
            content.Append("            PSDscAllowDomainUser        = $true;\r\n");
            content.Append("            #region Parameters\r\n");

            OrderedDictionary entries = GetEntries(state.ConfigurationData[node]);
            foreach (string key in SortedKeys(entries))
            {
                Hashtable entry = (Hashtable)entries[key];
                AppendDescription(content, entry);

                object? value = entry[ValueKey];
                string probe = value!.ToString();
                if (probe.StartsWith("@(", StringComparison.Ordinal) || probe.StartsWith("$", StringComparison.Ordinal))
                {
                    content.Append(EntryIndent).Append(key).Append(" = ").Append(PSInterop.ToPSString(value)).Append("\r\n\r\n");
                }
                else if (string.Equals(PSInterop.BaseTypeOf(value)!.FullName, "System.Object[]", StringComparison.Ordinal))
                {
                    content.Append(EntryIndent).Append(key).Append(" = ").Append(ConvertToConfigurationDataString(value));
                }
                else
                {
                    content.Append(EntryIndent).Append(key).Append(" = \"").Append(PSInterop.ToPSString(value)).Append("\"\r\n\r\n");
                }
            }

            content.Append("        },\r\n");
        }

        private static void AppendNonNodeData(StringBuilder content, ModuleState state, object node, Action<string>? warn)
        {
            content.Append("        @{\r\n");

            OrderedDictionary entries = GetEntries(state.ConfigurationData[node]);
            foreach (string key in SortedKeys(entries))
            {
                Hashtable entry = (Hashtable)entries[key];
                string? text = AsNonNodeDataText(entry[ValueKey]);
                if (text is null)
                {
                    warn?.Invoke("Could not obtain value for key " + key);
                    continue;
                }

                AppendDescription(content, entry);
                if (text.StartsWith("@(", StringComparison.Ordinal) || text.StartsWith("$", StringComparison.Ordinal))
                {
                    content.Append(EntryIndent).Append(key).Append(" = ").Append(text).Append("\r\n\r\n");
                }
                else
                {
                    content.Append(EntryIndent).Append(key).Append(" = \"").Append(text).Append("\"\r\n\r\n");
                }
            }

            content.Append("        }\r\n");
        }

        private static string? AsNonNodeDataText(object? value)
        {
            object? unwrapped = PSInterop.Unwrap(value);
            if (unwrapped is null)
            {
                return null;
            }

            if (unwrapped is object?[] items)
            {
                StringBuilder array = new("@(");
                for (int index = 0; index < items.Length; index++)
                {
                    if (index > 0)
                    {
                        array.Append(',');
                    }

                    array.Append('"').Append(PSInterop.ToPSString(items[index])).Append('"');
                }

                return array.Append(')').ToString();
            }

            return unwrapped as string;
        }

        private static void AppendDescription(StringBuilder content, Hashtable entry)
        {
            string description = PSInterop.ToPSString(entry[DescriptionKey]);
            if (!string.IsNullOrEmpty(description))
            {
                content.Append(EntryIndent).Append("# ").Append(description).Append("\r\n");
            }
        }

        private static IEnumerable<string> SortedKeys(OrderedDictionary entries)
        {
            return entries.Keys.Cast<object>()
                .Select(PSInterop.ToPSString)
                .OrderBy(key => key, EntryKeyComparer.Instance);
        }

        private static OrderedDictionary GetEntries(object? node)
        {
            return (OrderedDictionary)((Hashtable)node!)[EntriesKey]!;
        }

        private static void TrimTrailingComma(StringBuilder content)
        {
            if (content.Length >= 3 && content[content.Length - 3] == ',' && content[content.Length - 2] == '\r' && content[content.Length - 1] == '\n')
            {
                content.Remove(content.Length - 3, 1);
            }
        }

        private sealed class EntryKeyComparer : IComparer<string>
        {
            internal static readonly EntryKeyComparer Instance = new();

            public int Compare(string? x, string? y)
            {
                return PSInterop.CompareNames(x, y);
            }
        }
    }
}
