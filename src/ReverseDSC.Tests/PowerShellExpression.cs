using System.Management.Automation.Language;

namespace ReverseDSC.Tests;

internal static class PowerShellExpression
{
    internal static string Evaluate(string expression)
    {
        ScriptBlockAst ast = Parser.ParseInput(expression, out Token[] _, out ParseError[] errors);
        if (errors.Length > 0)
        {
            throw new InvalidOperationException($"'{expression}' does not parse: {errors[0].Message}");
        }

        if (ast
            .Find(candidate => candidate is StringConstantExpressionAst, true) is not StringConstantExpressionAst constant)
        {
            throw new InvalidOperationException($"'{expression}' is not a constant string expression.");
        }

        return constant.Value;
    }
}
