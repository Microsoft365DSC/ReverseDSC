using System.Collections;
using Xunit;

namespace ReverseDSC.Tests;

public class DscGeneratorTests : IDisposable
{
    private readonly ModuleState _state = TestData.NewState();
    private readonly string _modulePath = Path.Combine(Path.GetTempPath(), $"ReverseDSCResource{Guid.NewGuid():N}.psm1");

    public DscGeneratorTests()
    {
        File.WriteAllText(_modulePath, """
            function Get-TargetResource
            {
                param(
                    [ValidateSet('One', 'Two', 'Three')]
                    [System.String] $Mode,

                    [ValidateRange(5, 10)]
                    [System.UInt32] $Port,

                    [System.String] $Name,

                    [System.UInt32] $Count,

                    [System.Management.Automation.PSCredential] $Credential,

                    [System.Boolean] $Enabled,

                    [System.String[]] $Tags,

                    [Microsoft.Management.Infrastructure.CimInstance[]] $Members
                )
            }

            function Set-TargetResource
            {
                param(
                    [System.String] $SystemStringParam,
                    [string] $StringParam,
                    [System.Boolean] $BooleanParam,
                    [bool] $ShortBooleanParam,
                    [System.String[]] $StringArrayParam,
                    [string[]] $LowerStringArrayParam,
                    [Microsoft.Management.Infrastructure.CimInstance] $CimParam,
                    [Microsoft.Management.Infrastructure.CimInstance[]] $CimArrayParam,
                    [ValidateSet('A', 'B')][System.String] $ValidatedParam
                )
            }
            """);
    }

    [Fact]
    public void PadsTheParameterNamesToTheDefaultWidth()
    {
        Hashtable parameters = new() { { "Name", "TestResource" } };
        Assert.Equal("            Name                 = \"TestResource\";\r\n", GenerateBlock(parameters));
    }

    [Fact]
    public void PadsTheParameterNamesToTheLongestName()
    {
        Hashtable parameters = new()
        {
            { "Name", "Test" },
            { "ThisParameterNameIsLongerThanTwentyCharacters", "Test" },
        };

        string block = GenerateBlock(parameters);

        Assert.Contains("Name                                          = \"Test\";", block, StringComparison.Ordinal);
        Assert.Contains("ThisParameterNameIsLongerThanTwentyCharacters = \"Test\";", block, StringComparison.Ordinal);
    }

    [Fact]
    public void EmitsTheParametersInAlphabeticalOrder()
    {
        Hashtable parameters = new() { { "Zeta", "z" }, { "Alpha", "a" }, { "Mike", "m" } };
        string[] names = [.. GenerateBlock(parameters)
            .Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().Split(' ')[0])];

        Assert.Equal(["Alpha", "Mike", "Zeta"], names);
    }

    [Fact]
    public void ExcludesTheMetadataKeyAndAppendsItAsAComment()
    {
        Hashtable parameters = new() { { "Name", "Test" }, { "_metadata_Name", "# This is a comment" } };
        Assert.Equal("            Name                 = \"Test\"; # This is a comment\r\n", GenerateBlock(parameters));
    }

    [Fact]
    public void ExcludesTheParametersWithANullValue()
    {
        Hashtable parameters = new() { { "Name", "Test" }, { "Items", null } };
        Assert.DoesNotContain("Items", GenerateBlock(parameters), StringComparison.Ordinal);
    }

    [Fact]
    public void HonoursNoEscapeCaseInsensitivelyAndForOnlyTheListedParameters()
    {
        Hashtable parameters = new() { { "Name", "$ConfigName" }, { "Other", "$OtherName" } };
        string block = GenerateBlock(parameters, ["NAME"]);

        Assert.Contains("Name                 = $ConfigName;", block, StringComparison.Ordinal);
        Assert.Contains("Other                = \"`$OtherName\";", block, StringComparison.Ordinal);
    }

    [Fact]
    public void TreatsAMissingNoEscapeListAsEmpty()
    {
        Hashtable parameters = new() { { "Name", "$ConfigName" } };
        Assert.Contains("\"`$ConfigName\"", GenerateBlock(parameters, null), StringComparison.Ordinal);
    }

    [Fact]
    public void KeepsVariableReferencesWhenVariablesAreAllowed()
    {
        Hashtable parameters = new() { { "Name", "Value of $Node" } };
        string block = DscBlockGenerator.GenerateBlock(_state, parameters, null, true);

        Assert.Equal("            Name                 = \"Value of $Node\";\r\n", block);
    }

    [Fact]
    public void GeneratesAnEmptyBlockForEmptyParameters()
    {
        Assert.Equal(string.Empty, GenerateBlock([]));
    }

    [Theory]
    [InlineData("[xWebsite]DefaultSite", "@(\"[xWebsite]DefaultSite\");")]
    public void GeneratesADependsOnClauseForASingleDependency(string item, string expected)
    {
        Assert.Equal(expected, DscBlockGenerator.GenerateDependsOnBlock([item]));
    }

    [Fact]
    public void GeneratesADependsOnClauseForMultipleDependencies()
    {
        Assert.Equal(
            "@(\"[xWebsite]DefaultSite\",\"[xSPSite]MainSite\");",
            DscBlockGenerator.GenerateDependsOnBlock(["[xWebsite]DefaultSite", "[xSPSite]MainSite"]));
    }

    [Theory]
    [InlineData("$SystemStringParam", "System.String")]
    [InlineData("$StringParam", "System.String")]
    [InlineData("$BooleanParam", "System.Boolean")]
    [InlineData("$ShortBooleanParam", "System.Boolean")]
    [InlineData("$StringArrayParam", "System.String[]")]
    [InlineData("$LowerStringArrayParam", "System.String[]")]
    [InlineData("$CimParam", "System.Collections.Hashtable")]
    [InlineData("$CimArrayParam", "Microsoft.Management.Infrastructure.CimInstance[]")]
    [InlineData("$ValidatedParam", "System.String")]
    public void ResolvesTheDeclaredParameterType(string paramName, string expected)
    {
        Assert.Equal(expected, DscParamTypeResolver.Resolve(_state, _modulePath, paramName));
    }

    [Fact]
    public void ResolvesNothingForAnUnknownParameter()
    {
        Assert.Null(DscParamTypeResolver.Resolve(_state, _modulePath, "$NonExistent"));
    }

    [Fact]
    public void CachesTheSetTargetResourceParameters()
    {
        Assert.Equal("System.String", DscParamTypeResolver.Resolve(_state, _modulePath, "$StringParam"));
        Assert.True(_state.SetTargetResourceParameters.ContainsKey(_modulePath));
        Assert.Equal("System.String", DscParamTypeResolver.Resolve(_state, _modulePath, "$StringParam"));
    }

    [Fact]
    public void LeavesACimBlockAloneWhenTheParameterIsAbsent()
    {
        string block = "            Other = \"Value\";\r\n";
        Assert.Equal(block, DscStringParamConverter.Convert(block, "Members", true, false));
    }

    [Fact]
    public void LeavesACimBlockAloneWhenTheParameterHasNoTerminatingLineBreak()
    {
        string block = "            Members = @(MSFT_TeamMember{ Name = 'x' })";
        Assert.Equal(block, DscStringParamConverter.Convert(block, "Members", true, false));
    }

    [Fact]
    public void GeneratesFakeParametersFromTheDeclaredTypes()
    {
        Hashtable parameters = DscFakeParameterGenerator.Generate(_state, _modulePath);

        Assert.Equal("One", parameters["Mode"]);
        Assert.Equal("5", parameters["Port"]);
        Assert.Equal("*", parameters["Name"]);
        Assert.Equal(0, parameters["Count"]);
        Assert.True(parameters.ContainsKey("Credential"));
        Assert.Null(parameters["Credential"]);
        Assert.Equal(true, parameters["Enabled"]);
        Assert.Equal("1 2", parameters["Tags"]);
        Assert.False(parameters.ContainsKey("Members"));
        Assert.Equal(7, parameters.Count);
    }

    [Fact]
    public void GeneratesNoFakeParametersWithoutAGetTargetResourceFunction()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ReverseDSCNoGet{Guid.NewGuid():N}.psm1");
        File.WriteAllText(path, "function Set-TargetResource { param([System.String] $Name) }");

        try
        {
            Assert.Empty(DscFakeParameterGenerator.Generate(_state, path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RemovesTheQuotesAroundAParameterValue()
    {
        string block = "            ParamName            = \"SomeValue\";\r\n";
        Assert.Equal(
            "            ParamName            = SomeValue;\r\n",
            DscStringParamConverter.Convert(block, "ParamName", false, false));
    }

    [Fact]
    public void ReturnsTheBlockUnchangedWhenTheParameterIsNotFound()
    {
        string block = "            OtherParam           = \"Value\";\r\n";
        Assert.Equal(block, DscStringParamConverter.Convert(block, "NonExistent", false, false));
    }

    [Fact]
    public void ReturnsTheBlockUnchangedWhenTheValueHasNoClosingQuote()
    {
        string block = "            ParamName            = \"SomeValue;\r\n";
        Assert.Equal(block, DscStringParamConverter.Convert(block, "ParamName", false, false));
    }

    [Fact]
    public void FallsBackToTheSingleQuoteAsTheClosingDelimiter()
    {
        string block = "            ParamName            = \"SomeValue';\r\n";
        Assert.Equal(
            "            ParamName            = SomeValue;\r\n",
            DscStringParamConverter.Convert(block, "ParamName", false, false));
    }

    [Fact]
    public void OnlyUnquotesTheTargetParameter()
    {
        string block = "            ParamName = \"Value\";\r\n            Other = \"X\";\r\n";
        Assert.Equal(
            "            ParamName = Value;\r\n            Other = \"X\";\r\n",
            DscStringParamConverter.Convert(block, "ParamName", false, false));
    }

    [Fact]
    public void UnquotesAndUnescapesACimInstanceArray()
    {
        string block = TestData.Lines(
            "            Members              = \"@(MSFT_TeamMember{",
            "                DisplayName = `\"John Doe`\"",
            "                Role        = `\"Owner`\"",
            "            }",
            "            MSFT_TeamMember{",
            "                DisplayName = `\"Jane Roe`\"",
            "                Role        = `\"Member`\"",
            "            })\";",
            string.Empty);

        string expected = TestData.Lines(
            "            Members              = @(MSFT_TeamMember{",
            "                DisplayName = \"John Doe\"",
            "                Role        = \"Owner\"",
            "            }",
            "            MSFT_TeamMember{",
            "                DisplayName = \"Jane Roe\"",
            "                Role        = \"Member\"",
            "            });",
            string.Empty);

        Assert.Equal(expected, DscStringParamConverter.Convert(block, "Members", true, false));
    }

    [Fact]
    public void MovesTheClosingParenthesisOntoItsOwnLine()
    {
        string block = TestData.Lines(
            "            Members              = \"@(MSFT_TeamMember{",
            "                DisplayName = `\"John Doe",
            "            }\");",
            string.Empty);

        string expected = TestData.Lines(
            "            Members              = \"@(MSFT_TeamMember{",
            "                DisplayName = \"John Doe",
            "            }",
            "            );",
            string.Empty);

        Assert.Equal(expected, DscStringParamConverter.Convert(block, "Members", true, false));
    }

    [Fact]
    public void RemovesTheSeparatorLinesBetweenTheInstances()
    {
        string block = TestData.Lines(
            "            Members = @(MSFT_TeamMember{",
            "                Name = 'x'",
            "            },",
            "            MSFT_TeamMember{",
            "                Name = 'y'",
            "            });",
            string.Empty);

        string result = DscStringParamConverter.Convert(block, "Members", true, false);

        Assert.DoesNotContain("},\r\n", result, StringComparison.Ordinal);
        Assert.Contains("Name = 'x'", result, StringComparison.Ordinal);
    }

    [Fact]
    public void UnescapesTheAttributeQuotesOfACimObject()
    {
        string block = "            Content = \"<Rule Name=`\"Block`\" Action=`\"Deny`\" />\";\r\n";
        Assert.Equal(
            "            Content = <Rule Name=\"Block\" Action=\"Deny\" />;\r\n",
            DscStringParamConverter.Convert(block, "Content", false, true));
    }

    public void Dispose()
    {
        if (File.Exists(_modulePath))
        {
            File.Delete(_modulePath);
        }

        GC.SuppressFinalize(this);
    }

    private string GenerateBlock(Hashtable parameters, string[]? noEscape = null)
    {
        return DscBlockGenerator.GenerateBlock(_state, parameters, noEscape, false);
    }
}
