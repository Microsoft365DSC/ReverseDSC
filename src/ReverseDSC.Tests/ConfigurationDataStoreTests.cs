using System.Collections;
using Xunit;

namespace ReverseDSC.Tests;

public class ConfigurationDataStoreTests
{
    private readonly ModuleState _state = TestData.NewState();

    [Fact]
    public void ConvertsAStringObject()
    {
        Assert.Equal("\"TestValue\";\r\n", ConfigurationDataStore.ConvertToConfigurationDataString("TestValue"));
    }

    [Fact]
    public void ConvertsAnArrayOfStrings()
    {
        string expected = "            @(\r\n\"Item1\";\r\n\"Item2\";\r\n            )\r\n";
        Assert.Equal(expected, ConfigurationDataStore.ConvertToConfigurationDataString(new object[] { "Item1", "Item2" }));
    }

    [Fact]
    public void ConvertsAHashtable()
    {
        Hashtable value = new() { { "Name", "Test" } };
        string expected = "            @{\r\n                Name = \"Test\";\r\n            },\r\n";
        Assert.Equal(expected, ConfigurationDataStore.ConvertToConfigurationDataString(value));
    }

    [Fact]
    public void ConvertsAnArrayContainingAHashtableAndTrimsTheTrailingComma()
    {
        Hashtable inner = new() { { "A", "B" } };
        string expected = "            @(\r\n            @{\r\n                A = \"B\";\r\n            }\r\n            )\r\n";
        Assert.Equal(expected, ConfigurationDataStore.ConvertToConfigurationDataString(new object[] { inner }));
    }

    [Fact]
    public void ConvertsANestedArray()
    {
        object[] value = [new object[] { "x", "y" }];
        string result = ConfigurationDataStore.ConvertToConfigurationDataString(value);

        Assert.Contains("\"x\";\r\n", result, StringComparison.Ordinal);
        Assert.Contains("\"y\";\r\n", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ReturnsAnEmptyStringForAnUnsupportedType()
    {
        Assert.Equal(string.Empty, ConfigurationDataStore.ConvertToConfigurationDataString(42));
    }

    [Fact]
    public void WarnsAndSkipsTheEntryWhenANonNodeDataValueCannotBeConverted()
    {
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "Bad", "x", null);
        SetEntryValue("NonNodeData", "Bad", null);

        List<string> warnings = [];
        string content = ConfigurationDataStore.GetContent(_state, warnings.Add);

        Assert.Contains("Could not obtain value for key Bad", warnings);
        Assert.DoesNotContain("Bad =", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WarnsAndSkipsTheEntryWhenANonNodeDataValueIsNotAString()
    {
        ConfigurationDataStore.AddEntry(_state, "NonNodeData", "Count", 5, null);

        List<string> warnings = [];
        string content = ConfigurationDataStore.GetContent(_state, warnings.Add);

        Assert.Contains("Could not obtain value for key Count", warnings);
        Assert.DoesNotContain("Count =", content, StringComparison.Ordinal);
    }

    [Fact]
    public void StoresTheEntriesInACaseInsensitiveOrderedDictionary()
    {
        ConfigurationDataStore.AddEntry(_state, "localhost", "TenantId", "contoso", "The tenant");

        Hashtable? entry = ConfigurationDataStore.GetEntry(_state, "LOCALHOST", "tenantid");

        Assert.NotNull(entry);
        Assert.Equal("contoso", entry!["Value"]);
        Assert.Equal("The tenant", entry["Description"]);
    }

    private void SetEntryValue(string node, string key, object? value)
    {
        Hashtable entry = ConfigurationDataStore.GetEntry(_state, node, key)!;
        entry["Value"] = value;
    }
}
