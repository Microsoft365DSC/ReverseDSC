using System;
using System.Collections;
using System.Linq;
using System.Management.Automation.Language;

namespace ReverseDSC
{
    internal static class DscFakeParameterGenerator
    {
        private const string GetTargetResource = "Get-TargetResource";
        private static readonly string[] StringArrayPlaceholder = ["1", "2"];

        internal static Hashtable Generate(ModuleState state, string modulePath)
        {
            Hashtable parameters = ModuleState.NewHashtable();
            ScriptBlockAst ast = ModuleAstCache.GetModuleAst(state, modulePath);

            FunctionDefinitionAst? getTargetResource = AstQuery.FindFunctions(ast)
                .FirstOrDefault(function => PSInterop.AreEqual(function.Name, GetTargetResource));
            if (getTargetResource is null)
            {
                return parameters;
            }

            foreach (ParameterAst parameter in AstQuery.FindParameters(getTargetResource))
            {
                string paramName = parameter.Name.Extent.Text.Replace("$", string.Empty);
                if (TryAddValidationValue(parameters, paramName, parameter))
                {
                    continue;
                }

                AddDeclaredTypeValue(parameters, paramName, parameter);
            }

            return parameters;
        }

        private static bool TryAddValidationValue(Hashtable parameters, string paramName, ParameterAst parameter)
        {
            bool found = false;
            foreach (AttributeBaseAst attribute in parameter.Attributes)
            {
                if (attribute is not AttributeAst attributeAst || attributeAst.PositionalArguments.Count == 0)
                {
                    continue;
                }

                string firstArgument = attributeAst.PositionalArguments[0].ToString();
                if (PSInterop.AreEqual(attributeAst.TypeName.FullName, "ValidateSet"))
                {
                    parameters.Add(paramName, firstArgument.Replace("\"", string.Empty).Replace("'", string.Empty));
                    found = true;
                }
                else if (PSInterop.AreEqual(attributeAst.TypeName.FullName, "ValidateRange"))
                {
                    parameters.Add(paramName, firstArgument);
                    found = true;
                }
            }

            return found;
        }

        private static void AddDeclaredTypeValue(Hashtable parameters, string paramName, ParameterAst parameter)
        {
            foreach (AttributeBaseAst attribute in parameter.Attributes)
            {
                string typeName = attribute.TypeName.FullName;
                if (IsAnyOf(typeName, "System.String", "String"))
                {
                    parameters.Add(paramName, "*");
                    return;
                }

                if (IsAnyOf(typeName, "System.UInt32", "Int32"))
                {
                    parameters.Add(paramName, 0);
                    return;
                }

                if (IsAnyOf(typeName, "System.Management.Automation.PSCredential"))
                {
                    parameters.Add(paramName, null);
                    return;
                }

                if (IsAnyOf(typeName, "System.Management.Automation.Boolean", "System.Boolean", "Boolean"))
                {
                    parameters.Add(paramName, true);
                    return;
                }

                if (IsAnyOf(typeName, "System.String[]", "String[]"))
                {
                    parameters.Add(paramName, PSInterop.ToPSString(StringArrayPlaceholder));
                    return;
                }
            }
        }

        private static bool IsAnyOf(string typeName, params string[] candidates)
        {
            return candidates.Any(candidate => string.Equals(candidate, typeName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
