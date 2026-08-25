using Xunit;

namespace ReverseDSC.Tests;

public class DscClassInstanceConverterTests
{
    private readonly ModuleState _state = TestData.NewState();

    [Fact]
    public void RendersAMofStyleBlockWithAlignedProperties()
    {
        string expected = TestData.Lines(
            "MSFT_TestClassMember{",
            "                DisplayName = \"John Doe\"",
            "                Role        = \"Owner\"",
            "            }");

        Assert.Equal(expected, Convert(TestData.John()));
    }

    [Fact]
    public void RendersAnEmptyBlockWhenTheClassHasNoProperties()
    {
        Assert.Equal("MSFT_TestClassEmpty{\r\n            }", Convert(new MSFT_TestClassEmpty()));
    }

    [Fact]
    public void RendersAnEmptyBlockWhenNoPropertyWasSet()
    {
        Assert.Equal("MSFT_TestClassMember{\r\n            }", Convert(new MSFT_TestClassMember()));
    }

    [Fact]
    public void SkipsThePropertiesThatHaveNoValue()
    {
        MSFT_TestClassSetting instance = new() { Name = "Present" };
        Assert.Equal("MSFT_TestClassSetting{\r\n                Name = \"Present\"\r\n            }", Convert(instance));
    }

    [Fact]
    public void RendersThePropertiesInAlphabeticalOrder()
    {
        MSFT_TestClassSetting instance = new()
        {
            Name = "Costs $100 and \"quotes\"",
            Enabled = true,
            Ports = [80, 443],
            Tags = ["a", "b"],
        };

        string expected = TestData.Lines(
            "MSFT_TestClassSetting{",
            "                Enabled = $True",
            "                Name    = \"Costs `$100 and `\"quotes`\"\"",
            "                Ports   = @(80,443)",
            "                Tags    = @(\"a\",\"b\")",
            "            }");

        Assert.Equal(expected, Convert(instance));
    }

    [Fact]
    public void RendersOnlyTheDscPropertiesOfTheInstance()
    {
        MSFT_TestClassExtras instance = new() { Name = "Test", Filter = "ExportOnly", Secret = "Hidden" };
        Assert.Equal("MSFT_TestClassExtras{\r\n                Name = \"Test\"\r\n            }", Convert(instance));
    }

    [Fact]
    public void RendersANullableEnumPropertyAsAQuotedString()
    {
        MSFT_TestClassEnumHolder instance = new() { Color = ConsoleColor.Red };
        Assert.Equal("MSFT_TestClassEnumHolder{\r\n                Color = \"Red\"\r\n            }", Convert(instance));
    }

    [Fact]
    public void IndentsANestedInstanceRelativeToItsParent()
    {
        MSFT_TestClassTeam team = new() { DisplayName = "Contoso", Owner = TestData.John() };

        string expected = TestData.Lines(
            "MSFT_TestClassTeam{",
            "                DisplayName = \"Contoso\"",
            "                Owner       = MSFT_TestClassMember{",
            "                    DisplayName = \"John Doe\"",
            "                    Role        = \"Owner\"",
            "                }",
            "            }");

        Assert.Equal(expected, Convert(team));
    }

    [Fact]
    public void IndentsANestedArrayOfInstancesRelativeToItsParent()
    {
        MSFT_TestClassTeam team = new()
        {
            DisplayName = "Contoso",
            Members = [TestData.John(), TestData.Jane()],
        };

        string expected = TestData.Lines(
            "MSFT_TestClassTeam{",
            "                DisplayName = \"Contoso\"",
            "                Members     = @(",
            "                    MSFT_TestClassMember{",
            "                        DisplayName = \"John Doe\"",
            "                        Role        = \"Owner\"",
            "                    }",
            "                    MSFT_TestClassMember{",
            "                        DisplayName = \"Jane Roe\"",
            "                        Role        = \"Member\"",
            "                    }",
            "                )",
            "            }");

        Assert.Equal(expected, Convert(team));
    }

    [Theory]
    [MemberData(nameof(EmptyInstanceArrays))]
    public void RendersAnEmptyArray(object?[]? value)
    {
        Assert.Equal("@()", DscClassInstanceConverter.ConvertInstanceArray(_state, value, 12, false, false));
    }

    [Fact]
    public void SeparatesTheInstancesByALineBreakInsteadOfAComma()
    {
        string expected = TestData.Lines(
            "@(",
            "                MSFT_TestClassMember{",
            "                    DisplayName = \"John Doe\"",
            "                    Role        = \"Owner\"",
            "                }",
            "                MSFT_TestClassMember{",
            "                    DisplayName = \"Jane Roe\"",
            "                    Role        = \"Member\"",
            "                }",
            "            )");

        object?[] value = [TestData.John(), TestData.Jane()];
        Assert.Equal(expected, DscClassInstanceConverter.ConvertInstanceArray(_state, value, 12, false, false));
    }

    [Fact]
    public void SkipsTheNullElementsOfTheArray()
    {
        string expected = TestData.Lines(
            "@(",
            "                MSFT_TestClassMember{",
            "                    DisplayName = \"John Doe\"",
            "                    Role        = \"Owner\"",
            "                }",
            "            )");

        object?[] value = [TestData.John(), null];
        Assert.Equal(expected, DscClassInstanceConverter.ConvertInstanceArray(_state, value, 12, false, false));
    }

    public static TheoryData<object?[]?> EmptyInstanceArrays() =>
    [
        null!,
        Array.Empty<object?>(),
        new object?[] { null },
    ];

    private string Convert(object instance)
    {
        return DscClassInstanceConverter.ConvertInstance(_state, instance, 12, false, false);
    }
}
