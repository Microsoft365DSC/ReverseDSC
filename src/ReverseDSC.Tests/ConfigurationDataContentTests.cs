using System.Collections;
using System.Text.RegularExpressions;
using Xunit;

namespace ReverseDSC.Tests;

public partial class ConfigurationDataContentTests
{
    private readonly ModuleState _state = TestData.NewState();

    [Fact]
    public void WrapsTheContentInAnAllNodesAndANonNodeDataSection()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "ServerName", "MyServer", "The server name");

        string content = GetContent();

        Assert.StartsWith("@{\r\n    AllNodes = @(\r\n", content, StringComparison.Ordinal);
        Assert.EndsWith("    NonNodeData = @(\r\n    )\r\n}", content, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludesTheNodeNameAndTheDscCredentialSettings()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "ServerName", "MyServer", null);

        string content = GetContent();

        Assert.Contains("NodeName                    = \"localhost\"\r\n", content, StringComparison.Ordinal);
        Assert.Contains("PSDscAllowPlainTextPassword = $true;\r\n", content, StringComparison.Ordinal);
        Assert.Contains("PSDscAllowDomainUser        = $true;\r\n", content, StringComparison.Ordinal);
        Assert.Contains("#region Parameters\r\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public void QuotesAScalarNodeValueAndDocumentsIt()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "ServerName", "MyServer", "The server name");

        string content = GetContent();

        Assert.Contains("            # The server name\r\n", content, StringComparison.Ordinal);
        Assert.Contains("            ServerName = \"MyServer\"\r\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EmitsNodeValuesThatAreAlreadyArraysOrVariablesUnquoted()
    {
        ConfigurationDataStore.AddEntry(_state, "node1", "Feat", "@(\"a\",\"b\")", null);
        ConfigurationDataStore.AddEntry(_state, "node1", "Var", "$data", null);

        string content = GetContent();

        Assert.Contains("            Feat = @(\"a\",\"b\")\r\n", content, StringComparison.Ordinal);
        Assert.Contains("            Var = $data\r\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public void EmitsObjectArrayNodeValuesThroughTheRecursiveSerializer()
    {
        ConfigurationDataStore.AddEntry(_state, "node1", "Arr", new object[] { "x", "y" }, null);

        string content = GetContent();

        Assert.Contains("            Arr =             @(\r\n\"x\";\r\n\"y\";\r\n            )\r\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DoesNotEmitAnEmptyCommentForAnUndocumentedEntry()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "Undocumented", "Value", null);
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "AlsoUndocumented", "Value", string.Empty);

        string[] lines = GetContent().Split(["\r\n"], StringSplitOptions.None);

        Assert.DoesNotContain(lines, line => line.Trim() == "#");
    }

    [Fact]
    public void SeparatesTheNodesWithACommaAndLeavesNoTrailingOne()
    {
        ConfigurationDataStore.AddEntry(_state, "Server1", "Key1", "A", null);
        ConfigurationDataStore.AddEntry(_state, "Server2", "Key1", "B", null);

        string content = GetContent();

        Assert.Single(NodeSeparator().Matches(content));
        Assert.DoesNotContain("},\r\n    )", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersTheNonNodeDataSection()
    {
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "Thumbprint", "abc123", "cert thumbprint");
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "Servers", new object[] { "s1", "s2" }, null);
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "RawList", "@(\"a\",\"b\")", null);

        string content = GetContent();

        Assert.Contains("            # cert thumbprint\r\n", content, StringComparison.Ordinal);
        Assert.Contains("            Thumbprint = \"abc123\"\r\n", content, StringComparison.Ordinal);
        Assert.Contains("            Servers = @(\"s1\",\"s2\")\r\n", content, StringComparison.Ordinal);
        Assert.Contains("            RawList = @(\"a\",\"b\")\r\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SortsTheEntriesOfANodeByName()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "Zeta", "z", null);
        ConfigurationDataStore.AddEntry(_state, "localhost", "Alpha", "a", null);
        ConfigurationDataStore.AddEntry(_state, "localhost", "Mike", "m", null);

        string content = GetContent();

        Assert.True(content.IndexOf("Alpha", StringComparison.Ordinal) < content.IndexOf("Mike", StringComparison.Ordinal));
        Assert.True(content.IndexOf("Mike", StringComparison.Ordinal) < content.IndexOf("Zeta", StringComparison.Ordinal));
    }

    [Fact]
    public void ReturnsAnEmptyDocumentWhenNothingWasAdded()
    {
        Assert.Equal("@{\r\n    AllNodes = @(\r\n    )\r\n    NonNodeData = @(\r\n    )\r\n}", GetContent());
    }

    [Fact]
    public void UpdatesTheValueWhenTheSameKeyIsAddedTwice()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "Setting1", "Value1", null);
        ConfigurationDataStore.AddEntry(_state, "localhost", "Setting1", "Value2", null);

        Assert.Equal("Value2", ConfigurationDataStore.GetEntry(_state, "localhost", "Setting1")!["Value"]);
    }

    [Fact]
    public void FindsNothingForAnUnknownNodeOrKey()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "TestKey", "TestValue", null);

        Assert.Null(ConfigurationDataStore.GetEntry(_state, "UnknownNode", "TestKey"));
        Assert.Null(ConfigurationDataStore.GetEntry(_state, "localhost", "NonExistent"));
        Assert.Null(ConfigurationDataStore.GetEntry(_state, null, "NonExistent"));
        Assert.Null(ConfigurationDataStore.GetEntry(_state, string.Empty, "NonExistent"));
    }

    [Fact]
    public void SearchesEveryNodeWhenNoneIsGiven()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "TestKey", "TestValue", null);
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "Thumbprint", "abc123", null);

        Assert.Equal("TestValue", ConfigurationDataStore.GetEntry(_state, null, "TestKey")!["Value"]);
        Assert.Equal("abc123", ConfigurationDataStore.GetEntry(_state, string.Empty, "Thumbprint")!["Value"]);
    }

    [Fact]
    public void WritesTheDocumentAsUtf8WithoutABomAndATrailingLineBreak()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "TenantId", "contoso", null);
        string path = Path.Combine(Path.GetTempPath(), $"ReverseDSCDocument{Guid.NewGuid():N}.psd1");

        try
        {
            ConfigurationDataStore.WriteDocument(_state, path, null);

            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal((byte)'@', bytes[0]);
            Assert.Equal([(byte)'}', (byte)'\r', (byte)'\n'], bytes[^3..]);
            Assert.Equal(GetContent() + "\r\n", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void KeepsAnExistingNodeWhenASecondEntryIsAdded()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "First", "1", null);
        ConfigurationDataStore.AddEntry(_state, "localhost", "Second", "2", null);

        Assert.Equal("1", ConfigurationDataStore.GetEntry(_state, "localhost", "First")!["Value"]);
        Assert.Equal("2", ConfigurationDataStore.GetEntry(_state, "localhost", "Second")!["Value"]);
        Assert.Single(_state.ConfigurationData.Keys.Cast<object>());
    }

    [Fact]
    public void ClearingRemovesEveryNode()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "TenantId", "contoso", null);
        _state.ClearConfigurationData();

        Assert.Empty((IEnumerable)_state.ConfigurationData.Keys);
        Assert.Null(ConfigurationDataStore.GetEntry(_state, "localhost", "TenantId"));
    }

    private string GetContent()
    {
        return ConfigurationDataStore.GetContent(_state, null);
    }

    [GeneratedRegex("\\},\r\n")]
    private static partial Regex NodeSeparator();
}
