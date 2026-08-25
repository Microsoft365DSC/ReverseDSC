using System.Collections;
using System.Management.Automation;

namespace ReverseDSC.Commands
{
    /// <summary>
    /// Generates the DSC string representing an instance of the specified resource.
    /// CIM instances, class based complex type instances and arrays of either are rendered as MOF
    /// style blocks that are already unquoted and unescaped. Only values that were passed in as a
    /// pre-built string still need to be run through Convert-DSCStringParamToVariable afterwards.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DSCBlock")]
    [OutputType(typeof(string))]
    public sealed class GetDscBlockCommand : ReverseDscCmdlet
    {
        /// <summary>
        /// Full file path to the .psm1 module the instance is generated for. In most cases this is
        /// the full path to the .psm1 file of the DSC resource.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string ModulePath { get; set; } = string.Empty;

        /// <summary>Key properties of the instance and their values.</summary>
        [Parameter(Mandatory = true)]
        public Hashtable Params { get; set; } = null!;

        /// <summary>Names of the parameters whose values are not escaped.</summary>
        [Parameter]
        public string[]? NoEscape { get; set; }

        /// <summary>
        /// Preserves PowerShell variables inside string values instead of escaping them.
        /// </summary>
        [Parameter]
        public SwitchParameter AllowVariablesInStrings { get; set; }

        /// <summary>Writes the DSC representation of the instance.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(DscBlockGenerator.GenerateBlock(State, Params, NoEscape, AllowVariablesInStrings.IsPresent));
        }
    }

    /// <summary>
    /// Generates the DependsOn clause for the received list of dependencies.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DSCDependsOnBlock")]
    [OutputType(typeof(string))]
    public sealed class GetDscDependsOnBlockCommand : ReverseDscCmdlet
    {
        /// <summary>
        /// Dependencies of the current DSC block, in the form [&lt;DSCResourceName&gt;]&lt;InstanceName&gt;.
        /// </summary>
        [Parameter(Mandatory = true)]
        public object[] DependsOnItems { get; set; } = null!;

        /// <summary>Writes the DependsOn clause.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(DscBlockGenerator.GenerateDependsOnBlock(DependsOnItems));
        }
    }

    /// <summary>
    /// Retrieves the data type of a parameter of the associated DSC resource, as it is declared on
    /// the Set-TargetResource function of the module.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DSCParamType")]
    [OutputType(typeof(string))]
    public sealed class GetDscParamTypeCommand : ReverseDscCmdlet
    {
        /// <summary>Full file path to the .psm1 module that declares the parameter.</summary>
        [Parameter(Mandatory = true)]
        public string ModulePath { get; set; } = string.Empty;

        /// <summary>Name of the parameter, including its leading dollar sign.</summary>
        [Parameter(Mandatory = true)]
        public string ParamName { get; set; } = string.Empty;

        /// <summary>Writes the declared data type, or nothing when the parameter is unknown.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(DscParamTypeResolver.Resolve(State, ModulePath, ParamName));
        }
    }

    /// <summary>
    /// Generates a hashtable holding every property the Get-TargetResource function of the
    /// specified DSC resource exposes, with a fake value derived from the declared data type.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "DSCFakeParameters")]
    [OutputType(typeof(Hashtable))]
    public sealed class GetDscFakeParametersCommand : ReverseDscCmdlet
    {
        /// <summary>Full file path to the .psm1 module of the DSC resource.</summary>
        [Parameter(Mandatory = true)]
        public string ModulePath { get; set; } = string.Empty;

        /// <summary>Writes the hashtable of fake parameter values.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(DscFakeParameterGenerator.Generate(State, ModulePath));
        }
    }

    /// <summary>
    /// Removes the quotes around a parameter value in a DSC block, turning the value into a
    /// variable reference instead of a string. Class based complex types and CIM instances that
    /// were rendered by Get-DSCBlock are already unquoted and must not be passed through this.
    /// </summary>
    [Cmdlet(VerbsData.Convert, "DSCStringParamToVariable")]
    [OutputType(typeof(string))]
    public sealed class ConvertDscStringParamToVariableCommand : ReverseDscCmdlet
    {
        /// <summary>The DSC block of the resource instance being extracted.</summary>
        [Parameter(Mandatory = true)]
        public string DSCBlock { get; set; } = string.Empty;

        /// <summary>Name of the parameter whose value becomes a variable.</summary>
        [Parameter(Mandatory = true)]
        public string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// Marks the value as an array of CIM instances, whose items are not separated by commas
        /// and whose nested properties carry escaped double quotes.
        /// </summary>
        [Parameter]
        public bool IsCIMArray { get; set; }

        /// <summary>
        /// Marks the value as a single CIM instance, which is a string carrying escaped double
        /// quotes that have to be handled separately.
        /// </summary>
        [Parameter]
        public bool IsCIMObject { get; set; }

        /// <summary>Writes the DSC block with the value of the parameter unquoted.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(DscStringParamConverter.Convert(DSCBlock, ParameterName, IsCIMArray, IsCIMObject));
        }
    }
}
