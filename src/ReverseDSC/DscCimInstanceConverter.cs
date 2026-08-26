using System.Collections.Generic;
using System.Text;
using Microsoft.Management.Infrastructure;

namespace ReverseDSC
{
    internal static class DscCimInstanceConverter
    {
        internal static string ConvertInstance(ModuleState state, CimInstance value, int indent, bool noEscape, bool allowVariables)
        {
            StringBuilder builder = new();
            AppendInstance(builder, state, value, indent, noEscape, allowVariables);
            return builder.ToString();
        }

        internal static string ConvertInstanceArray(ModuleState state, CimInstance[]? value, int indent, bool noEscape, bool allowVariables)
        {
            StringBuilder builder = new();
            AppendInstanceArray(builder, state, value, indent, noEscape, allowVariables);
            return builder.ToString();
        }

        internal static void AppendInstance(StringBuilder builder, ModuleState state, CimInstance value, int indent, bool noEscape, bool allowVariables)
        {
            List<CimProperty> properties = [];
            int maximumNameLength = 0;
            foreach (CimProperty property in value.CimInstanceProperties)
            {
                if (property.Value is null)
                {
                    continue;
                }

                properties.Add(property);
                if (property.Name.Length > maximumNameLength)
                {
                    maximumNameLength = property.Name.Length;
                }
            }

            builder.Append(value.CimSystemProperties.ClassName).Append("{\r\n");
            string closingIndent = PSInterop.Indent(indent);
            if (properties.Count == 0)
            {
                builder.Append(closingIndent).Append('}');
                return;
            }

            string propertyIndent = PSInterop.Indent(indent + 4);
            for (int index = 0; index < properties.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append("\r\n");
                }

                CimProperty property = properties[index];
                builder.Append(propertyIndent).Append(property.Name.PadRight(maximumNameLength)).Append(" = ");
                DscValueConverter.AppendValue(builder, state, property.Value, property.Name, indent + 4, noEscape, allowVariables);
            }

            builder.Append("\r\n").Append(closingIndent).Append('}');
        }

        internal static void AppendInstanceArray(StringBuilder builder, ModuleState state, CimInstance[]? value, int indent, bool noEscape, bool allowVariables)
        {
            if (value is null || value.Length == 0 || (value.Length == 1 && value[0] is null))
            {
                builder.Append("@()");
                return;
            }

            string instanceIndent = PSInterop.Indent(indent + 4);
            builder.Append("@(\r\n");
            for (int index = 0; index < value.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append("\r\n");
                }

                builder.Append(instanceIndent);
                AppendInstance(builder, state, value[index], indent + 4, noEscape, allowVariables);
            }

            builder.Append("\r\n").Append(PSInterop.Indent(indent)).Append(')');
        }
    }
}
