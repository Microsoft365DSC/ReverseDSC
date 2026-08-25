using System.Collections;
using System.Management.Automation;
using Microsoft.Management.Infrastructure;
using Xunit;

namespace ReverseDSC.Tests;

public class DscValueDispatchTests
{
    private readonly ModuleState _state = TestData.NewState();
    private static readonly int[] Ports = [80, 443];

    [Fact]
    public void DispatchesACredentialToTheCredentialConverter()
    {
        Assert.Equal("$Credssvc_admin", Convert(TestData.Credential("CONTOSO\\svc-admin")));
    }

    [Fact]
    public void DispatchesAHashtableToTheHashtableConverter()
    {
        Hashtable value = new() { { "SubKey", "SubValue" } };
        Assert.Equal("@{SubKey = \"SubValue\"; }", Convert(value));
    }

    [Fact]
    public void DispatchesACimInstanceToTheCimConverter()
    {
        string expected = TestData.Lines(
            "MSFT_TeamMember{",
            "                DisplayName = \"John Doe\"",
            "                Role        = \"Owner\"",
            "            }");

        Assert.Equal(expected, Convert(TestData.CimJohn()));
    }

    [Fact]
    public void DispatchesACimInstanceArrayToTheCimArrayConverter()
    {
        CimInstance[] value = [TestData.CimJohn()];
        string expected = TestData.Lines(
            "@(",
            "                MSFT_TeamMember{",
            "                    DisplayName = \"John Doe\"",
            "                    Role        = \"Owner\"",
            "                }",
            "            )");

        Assert.Equal(expected, Convert(value));
    }

    [Fact]
    public void DispatchesAnObjectArrayToTheObjectArrayConverter()
    {
        object[] value = ["x", "y"];
        Assert.Equal("@(\"x\",\"y\")", Convert(value));
    }

    [Fact]
    public void DispatchesAClassInstanceAndAClassInstanceArray()
    {
        Assert.StartsWith("MSFT_TestClassMember{", Convert(TestData.John()), StringComparison.Ordinal);
        Assert.StartsWith("@(\r\n", Convert(new[] { TestData.John() }), StringComparison.Ordinal);
    }

    [Fact]
    public void DispatchesTheScalarTypesThatRenderAsQuotedStrings()
    {
        Assert.Equal("\"text\"", Convert("text"));
        Assert.Equal("\"0f8fad5b-d9cb-469f-a165-70867728950e\"", Convert(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")));
        Assert.Equal("\"01:30:00\"", Convert(TimeSpan.FromMinutes(90)));
    }

    [Fact]
    public void DispatchesAnIntegerArray()
    {
        Assert.Equal("@(80,443)", Convert(Ports));
    }

    [Fact]
    public void UnwrapsAPSObjectBeforeDispatchingButStringifiesTheOriginal()
    {
        PSObject custom = new();
        custom.Properties.Add(new PSNoteProperty("Z", 1));

        Assert.Equal("@{Z=1}", Convert(custom));
        Assert.Equal("$True", Convert(PSObject.AsPSObject(true)));
    }

    [Fact]
    public void SeparatesThePairsOfAMultiKeyHashtableInsideAnObjectArray()
    {
        Hashtable first = new() { { "A", "B" }, { "C", "D" } };
        string result = DscValueConverter.ConvertObjectArray([first], false, false);

        Assert.Contains("; ", result, StringComparison.Ordinal);
        Assert.Contains("A='B'", result, StringComparison.Ordinal);
        Assert.Contains("C='D'", result, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersAnUnknownTypeThroughThePowerShellStringConversion()
    {
        Assert.Equal("1.5", Convert(1.5));
        Assert.Equal("9999999999", Convert(9999999999L));
    }

    private string Convert(object value)
    {
        return DscValueConverter.ConvertValue(_state, value, "Parameter", 12, false, false);
    }
}
