using System.Collections;
using System.Management.Automation;

namespace ReverseDSC.Commands
{
    /// <summary>
    /// Adds a property to the ConfigurationData file the extract generates. The description ends up
    /// as a comment on top of the property in the resulting .psd1 file.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "ConfigurationDataEntry")]
    public sealed class AddConfigurationDataEntryCommand : ReverseDscCmdlet
    {
        /// <summary>
        /// Node the property is added under. NonNodeData adds it to the non-node specific section.
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Node { get; set; } = string.Empty;

        /// <summary>Name of the property.</summary>
        [Parameter(Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Value of the property.</summary>
        [Parameter(Mandatory = true)]
        public object Value { get; set; } = null!;

        /// <summary>Comment written on top of the property.</summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary>Stores the property. Writes nothing.</summary>
        protected override void ProcessRecord()
        {
            ConfigurationDataStore.AddEntry(State, Node, Key, Value, Description);
        }
    }

    /// <summary>
    /// Retrieves the value and the description of a property of the ConfigurationData content that
    /// is being built.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ConfigurationDataEntry")]
    [OutputType(typeof(Hashtable))]
    public sealed class GetConfigurationDataEntryCommand : ReverseDscCmdlet
    {
        /// <summary>
        /// Node to look in. When omitted or empty, every node is searched and the first match wins.
        /// </summary>
        [Parameter]
        [AllowNull]
        [AllowEmptyString]
        public string? Node { get; set; }

        /// <summary>Name of the property to retrieve.</summary>
        [Parameter(Mandatory = true)]
        public string Key { get; set; } = string.Empty;

        /// <summary>Writes the entry, or nothing when the property is unknown.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(ConfigurationDataStore.GetEntry(State, Node, Key));
        }
    }

    /// <summary>
    /// Clears the ConfigurationData content that is being built, resetting it to an empty state.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "ConfigurationDataContent")]
    public sealed class ClearConfigurationDataContentCommand : ReverseDscCmdlet
    {
        /// <summary>Empties the content. Writes nothing.</summary>
        protected override void ProcessRecord()
        {
            State.ClearConfigurationData();
        }
    }

    /// <summary>
    /// Returns the ConfigurationData content that is being built as the formatted string of a
    /// .psd1 file, with an AllNodes and a NonNodeData section.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ConfigurationDataContent")]
    [OutputType(typeof(string))]
    public sealed class GetConfigurationDataContentCommand : ReverseDscCmdlet
    {
        /// <summary>Writes the ConfigurationData content.</summary>
        protected override void ProcessRecord()
        {
            WriteObject(ConfigurationDataStore.GetContent(State, WriteWarning));
        }
    }

    /// <summary>
    /// Writes the ConfigurationData content that is being built to a .psd1 file, encoded as UTF-8
    /// without a byte order mark.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConfigurationDataDocument")]
    public sealed class NewConfigurationDataDocumentCommand : ReverseDscCmdlet
    {
        /// <summary>Full file path the resulting file is written to.</summary>
        [Parameter(Mandatory = true)]
        public string Path { get; set; } = string.Empty;

        /// <summary>Writes the file. Writes nothing to the pipeline.</summary>
        protected override void ProcessRecord()
        {
            string resolvedPath = GetUnresolvedProviderPathFromPSPath(Path);
            ConfigurationDataStore.WriteDocument(State, resolvedPath, WriteWarning);
        }
    }
}
