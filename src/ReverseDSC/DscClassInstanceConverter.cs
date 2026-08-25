using System.Collections.Generic;
using System.Text;

namespace ReverseDSC
{
    internal static class DscClassInstanceConverter
    {
        internal static string ConvertInstance(ModuleState state, object value, int indent, bool noEscape, bool allowVariables)
        {
            StringBuilder builder = new();
            AppendInstance(builder, state, value, indent, noEscape, allowVariables);
            return builder.ToString();
        }

        internal static string ConvertInstanceArray(ModuleState state, object?[]? value, int indent, bool noEscape, bool allowVariables)
        {
            StringBuilder builder = new();
            AppendInstanceArray(builder, state, value, indent, noEscape, allowVariables);
            return builder.ToString();
        }

        internal static void AppendInstance(StringBuilder builder, ModuleState state, object value, int indent, bool noEscape, bool allowVariables)
        {
            List<DscInstanceProperty> properties = DscClassTypeInspector.GetInstanceProperties(value);

            builder.Append(PSInterop.BaseTypeOf(value)!.Name).Append("{\r\n");
            string closingIndent = PSInterop.Indent(indent);
            if (properties.Count == 0)
            {
                builder.Append(closingIndent).Append('}');
                return;
            }

            int maximumNameLength = 0;
            foreach (DscInstanceProperty property in properties)
            {
                if (property.Name.Length > maximumNameLength)
                {
                    maximumNameLength = property.Name.Length;
                }
            }

            string propertyIndent = PSInterop.Indent(indent + 4);
            for (int index = 0; index < properties.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append("\r\n");
                }

                DscInstanceProperty property = properties[index];
                builder.Append(propertyIndent).Append(property.Name.PadRight(maximumNameLength)).Append(" = ");
                DscValueConverter.AppendValue(builder, state, property.Value, property.Name, indent + 4, noEscape, allowVariables);
            }

            builder.Append("\r\n").Append(closingIndent).Append('}');
        }

        internal static void AppendInstanceArray(StringBuilder builder, ModuleState state, object?[]? value, int indent, bool noEscape, bool allowVariables)
        {
            if (value is null || value.Length == 0)
            {
                builder.Append("@()");
                return;
            }

            string instanceIndent = PSInterop.Indent(indent + 4);
            int rendered = 0;
            int restorePosition = builder.Length;
            builder.Append("@(\r\n");

            foreach (object? instance in value)
            {
                if (instance is null)
                {
                    continue;
                }

                if (rendered > 0)
                {
                    builder.Append("\r\n");
                }

                builder.Append(instanceIndent);
                AppendInstance(builder, state, instance, indent + 4, noEscape, allowVariables);
                rendered++;
            }

            if (rendered == 0)
            {
                builder.Length = restorePosition;
                builder.Append("@()");
                return;
            }

            builder.Append("\r\n").Append(PSInterop.Indent(indent)).Append(')');
        }
    }
}
