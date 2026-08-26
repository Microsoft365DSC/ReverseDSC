using System.Collections.Specialized;
using Microsoft.Management.Infrastructure;
using Xunit;

namespace ReverseDSC.Tests;

public class DscCimInstanceConverterTests
{
    private readonly ModuleState _state = TestData.NewState();
    private static readonly int[] SettingPorts = [80, 443];
    private static readonly string[] SettingTags = ["a", "b"];

    [Fact]
    public void RendersAMofStyleBlockWithAlignedProperties()
    {
        string expected = TestData.Lines(
            "MSFT_TeamMember{",
            "                DisplayName = \"John Doe\"",
            "                Role        = \"Owner\"",
            "            }");

        Assert.Equal(expected, Convert(TestData.CimJohn()));
    }

    [Fact]
    public void RendersAnEmptyBlockWhenTheInstanceHasNoProperties()
    {
        CimInstance instance = new("MSFT_Empty", TestData.CimNamespace);
        Assert.Equal("MSFT_Empty{\r\n            }", Convert(instance));
    }

    [Fact]
    public void SkipsThePropertiesThatHaveNoValue()
    {
        CimInstance instance = new("MSFT_Setting", TestData.CimNamespace);
        instance.CimInstanceProperties.Add(CimProperty.Create("Name", "Present", CimFlags.Property));
        instance.CimInstanceProperties.Add(CimProperty.Create("Missing", null, CimType.String, CimFlags.Property));

        Assert.Equal("MSFT_Setting{\r\n                Name = \"Present\"\r\n            }", Convert(instance));
    }

    [Fact]
    public void ConvertsEachPropertyWithTheConverterOfItsOwnType()
    {
        CimInstance instance = TestData.NewCimInstance("MSFT_Setting", new OrderedDictionary
        {
            { "Name", "Costs $100 and \"quotes\"" },
            { "Enabled", true },
            { "Ports", SettingPorts },
            { "Tags", SettingTags },
        });

        string expected = TestData.Lines(
            "MSFT_Setting{",
            "                Name    = \"Costs `$100 and `\"quotes`\"\"",
            "                Enabled = $True",
            "                Ports   = @(80,443)",
            "                Tags    = @(\"a\",\"b\")",
            "            }");

        Assert.Equal(expected, Convert(instance));
    }

    [Fact]
    public void IndentsANestedInstanceRelativeToItsParent()
    {
        CimInstance team = new("MSFT_Team", TestData.CimNamespace);
        team.CimInstanceProperties.Add(CimProperty.Create("DisplayName", "Contoso", CimFlags.Property));
        team.CimInstanceProperties.Add(CimProperty.Create("Owner", TestData.CimJohn(), CimType.Instance, CimFlags.Property));

        string expected = TestData.Lines(
            "MSFT_Team{",
            "                DisplayName = \"Contoso\"",
            "                Owner       = MSFT_TeamMember{",
            "                    DisplayName = \"John Doe\"",
            "                    Role        = \"Owner\"",
            "                }",
            "            }");

        Assert.Equal(expected, Convert(team));
    }

    [Theory]
    [MemberData(nameof(EmptyInstanceArrays))]
    public void RendersAnEmptyArray(CimInstance[]? value)
    {
        Assert.Equal("@()", DscCimInstanceConverter.ConvertInstanceArray(_state, value, 12, false, false));
    }

    [Fact]
    public void SeparatesTheInstancesByALineBreakInsteadOfAComma()
    {
        string expected = TestData.Lines(
            "@(",
            "                MSFT_TeamMember{",
            "                    DisplayName = \"John Doe\"",
            "                    Role        = \"Owner\"",
            "                }",
            "                MSFT_TeamMember{",
            "                    DisplayName = \"Jane Roe\"",
            "                    Role        = \"Member\"",
            "                }",
            "            )");

        CimInstance[] value = [TestData.CimJohn(), TestData.CimJane()];
        Assert.Equal(expected, DscCimInstanceConverter.ConvertInstanceArray(_state, value, 12, false, false));
    }

    public static TheoryData<CimInstance[]?> EmptyInstanceArrays() =>
    [
        null!,
        Array.Empty<CimInstance>(),
        new CimInstance[] { null! },
    ];

    private string Convert(CimInstance instance)
    {
        return DscCimInstanceConverter.ConvertInstance(_state, instance, 12, false, false);
    }
}
