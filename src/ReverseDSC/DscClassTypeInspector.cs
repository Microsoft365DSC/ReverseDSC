using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace ReverseDSC
{
    internal readonly struct DscInstanceProperty
    {
        internal DscInstanceProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }

        internal string Name { get; }

        internal object Value { get; }
    }

    internal static class DscClassTypeInspector
    {
        private const string ComplexTypeNamePrefix = "MSFT_";

        internal static bool IsDscClassType(ModuleState state, Type? type)
        {
            if (type is null || type.IsArray || !type.IsClass)
            {
                return false;
            }

            if (state.DscClassTypes.TryGetValue(type, out bool cached))
            {
                return cached;
            }

            bool isClassType = type.Name.StartsWith(ComplexTypeNamePrefix, StringComparison.OrdinalIgnoreCase)
                || type.GetProperties().Any(HasDscPropertyAttribute);

            state.DscClassTypes[type] = isClassType;
            return isClassType;
        }

        internal static bool IsDscClassInstance(ModuleState state, object? value)
        {
            return value is not null && IsDscClassType(state, PSInterop.BaseTypeOf(value));
        }

        internal static bool IsDscClassInstanceArray(ModuleState state, object? value)
        {
            if (PSInterop.Unwrap(value) is not Array array)
            {
                return false;
            }

            if (IsDscClassType(state, array.GetType().GetElementType()))
            {
                return true;
            }

            foreach (object? item in array)
            {
                if (item is not null)
                {
                    return IsDscClassInstance(state, item);
                }
            }

            return false;
        }

        internal static List<DscInstanceProperty> GetInstanceProperties(object value)
        {
            List<DscInstanceProperty> properties = [];
            object instance = PSInterop.Unwrap(value)!;

            IEnumerable<PropertyInfo> declared = instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(property => property.Name, NameComparer.Instance);

            foreach (PropertyInfo property in declared)
            {
                if (!HasDscPropertyAttribute(property))
                {
                    continue;
                }

                object? propertyValue = property.GetValue(instance);
                if (propertyValue is null)
                {
                    continue;
                }

                properties.Add(new DscInstanceProperty(property.Name, propertyValue));
            }

            return properties;
        }

        private static bool HasDscPropertyAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(DscPropertyAttribute), true).Length > 0;
        }

        private sealed class NameComparer : IComparer<string>
        {
            internal static readonly NameComparer Instance = new();

            public int Compare(string? x, string? y)
            {
                return PSInterop.CompareNames(x, y);
            }
        }
    }
}
