using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;

namespace ReverseDSC
{
    internal static class DscParamTypeResolver
    {
        private const string SetTargetResource = "Set-TargetResource";

        private static readonly Dictionary<string, string> DeclaredTypeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Microsoft.Management.Infrastructure.CimInstance"] = "System.Collections.Hashtable",
                ["Microsoft.Management.Infrastructure.CimInstance[]"] = "Microsoft.Management.Infrastructure.CimInstance[]",
                ["string"] = "System.String",
                ["boolean"] = "System.Boolean",
                ["bool"] = "System.Boolean",
                ["string[]"] = "System.String[]",
            };

        internal static string? Resolve(ModuleState state, string modulePath, string paramName)
        {
            foreach (ParameterAst parameter in GetSetTargetResourceParameters(state, modulePath))
            {
                if (!PSInterop.AreEqual(parameter.Name.Extent.Text, paramName))
                {
                    continue;
                }

                foreach (AttributeBaseAst attribute in parameter.Attributes)
                {
                    string typeName = attribute.TypeName.FullName;
                    if (typeName.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                    {
                        return typeName;
                    }

                    if (DeclaredTypeMap.TryGetValue(typeName, out string mapped))
                    {
                        return mapped;
                    }
                }
            }

            return null;
        }

        private static ParameterAst[] GetSetTargetResourceParameters(ModuleState state, string modulePath)
        {
            if (state.SetTargetResourceParameters.TryGetValue(modulePath, out ParameterAst[] cached))
            {
                return cached;
            }

            ParameterAst[] parameters = [];
            ScriptBlockAst ast = ModuleAstCache.GetModuleAst(state, modulePath);
            foreach (FunctionDefinitionAst function in AstQuery.FindFunctions(ast))
            {
                if (PSInterop.AreEqual(function.Name, SetTargetResource))
                {
                    parameters = [.. AstQuery.FindParameters(function)];
                    break;
                }
            }

            state.SetTargetResourceParameters[modulePath] = parameters;
            return parameters;
        }
    }

    internal static class AstQuery
    {
        internal static IEnumerable<FunctionDefinitionAst> FindFunctions(Ast ast)
        {
            return ast.FindAll(candidate => candidate is FunctionDefinitionAst, true).Cast<FunctionDefinitionAst>();
        }

        internal static IEnumerable<ParameterAst> FindParameters(FunctionDefinitionAst function)
        {
            return function.Body.FindAll(candidate => candidate is ParameterAst, true).Cast<ParameterAst>();
        }
    }
}
