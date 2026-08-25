using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;

namespace ReverseDSC
{
    internal sealed class ModuleState
    {
        private static readonly ConditionalWeakTable<PSModuleInfo, ModuleState> States =
            new();

        private static readonly ModuleState Fallback = new();

        internal List<string> Credentials { get; } = [];

        internal List<string> UserNames { get; } = [];

        internal Hashtable ConfigurationData { get; private set; } = NewConfigurationData();

        internal Dictionary<string, ScriptBlockAst> ModuleAstCache { get; } =
            new Dictionary<string, ScriptBlockAst>(StringComparer.OrdinalIgnoreCase);

        internal Dictionary<string, ParameterAst[]> SetTargetResourceParameters { get; } =
            new Dictionary<string, ParameterAst[]>(StringComparer.OrdinalIgnoreCase);

        internal Dictionary<Type, bool> DscClassTypes { get; } = [];

        internal static ModuleState For(PSModuleInfo? module)
        {
            return module is null ? Fallback : States.GetValue(module, _ => new ModuleState());
        }

        internal void ClearConfigurationData()
        {
            ConfigurationData = NewConfigurationData();
        }

        internal void Reset()
        {
            Credentials.Clear();
            UserNames.Clear();
            ModuleAstCache.Clear();
            SetTargetResourceParameters.Clear();
            DscClassTypes.Clear();
            ClearConfigurationData();
        }

        internal static Hashtable NewHashtable()
        {
            return new Hashtable(StringComparer.OrdinalIgnoreCase);
        }

        private static Hashtable NewConfigurationData()
        {
            return NewHashtable();
        }
    }
}
