using System.Management.Automation.Language;

namespace ReverseDSC
{
    internal static class ModuleAstCache
    {
        internal static ScriptBlockAst GetModuleAst(ModuleState state, string modulePath)
        {
            if (!state.ModuleAstCache.TryGetValue(modulePath, out ScriptBlockAst ast))
            {
                ast = Parser.ParseFile(modulePath, out Token[] _, out ParseError[] _);
                state.ModuleAstCache[modulePath] = ast;
            }

            return ast;
        }
    }
}
