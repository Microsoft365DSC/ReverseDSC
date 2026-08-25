using System.Collections;
using System.Management.Automation;
using Xunit;

namespace ReverseDSC.Tests;

public class PSInteropTests
{
    [Fact]
    public void UnwrapReturnsTheBaseObjectOfAPSObject()
    {
        PSObject wrapped = PSObject.AsPSObject("text");
        Assert.Equal("text", PSInterop.Unwrap(wrapped));
        Assert.Equal("text", PSInterop.Unwrap("text"));
        Assert.Null(PSInterop.Unwrap(null));
    }

    [Fact]
    public void BaseTypeOfReportsTheTypeBehindAPSObject()
    {
        Assert.Equal(typeof(int), PSInterop.BaseTypeOf(PSObject.AsPSObject(1)));
        Assert.Null(PSInterop.BaseTypeOf(null));
    }

    private static readonly string[] StringArrayPlaceholder = ["1", "2"];

    [Fact]
    public void ToPSStringUsesThePowerShellConversion()
    {
        Assert.Equal(string.Empty, PSInterop.ToPSString(null));
        Assert.Equal("1 2", PSInterop.ToPSString(StringArrayPlaceholder));
        Assert.Equal("True", PSInterop.ToPSString(true));
    }

    [Fact]
    public void CompareNamesIsCaseInsensitive()
    {
        Assert.Equal(0, PSInterop.CompareNames("Alpha", "alpha"));
        Assert.True(PSInterop.CompareNames("Alpha", "Beta") < 0);
    }

    [Fact]
    public void ContainsNameIgnoresCasingAndHandlesAMissingList()
    {
        Assert.True(PSInterop.ContainsName(["Name"], "NAME"));
        Assert.False(PSInterop.ContainsName(["Name"], "Other"));
        Assert.False(PSInterop.ContainsName(null, "Name"));
    }

    [Fact]
    public void ToObjectArrayReturnsNullForNull()
    {
        Assert.Null(PSInterop.ToObjectArray(null));
    }

    [Fact]
    public void ToObjectArrayKeepsAnObjectArrayAsItIs()
    {
        object?[] value = ["a", "b"];
        Assert.Same(value, PSInterop.ToObjectArray(value));
    }

    [Fact]
    public void ToObjectArrayWrapsASingleString()
    {
        Assert.Equal(["text"], PSInterop.ToObjectArray("text"));
    }

    [Fact]
    public void ToObjectArrayFlattensAnEnumerable()
    {
        Assert.Equal(["a", "b"], PSInterop.ToObjectArray(new ArrayList { "a", "b" }));
    }

    [Fact]
    public void ToObjectArrayWrapsAScalar()
    {
        Assert.Equal([42], PSInterop.ToObjectArray(42));
    }

    [Fact]
    public void IndentReturnsTheRequestedNumberOfSpaces()
    {
        Assert.Equal(string.Empty, PSInterop.Indent(0));
        Assert.Equal(string.Empty, PSInterop.Indent(-4));
        Assert.Equal("    ", PSInterop.Indent(4));
    }

    [Fact]
    public void GetReturnsTheStoredUserNameOrNothing()
    {
        ModuleState state = TestData.NewState();
        Assert.Null(CredentialRepository.Get(state, "CONTOSO\\admin"));

        CredentialRepository.Save(state, "CONTOSO\\ADMIN");
        Assert.Equal("contoso\\admin", CredentialRepository.Get(state, "CONTOSO\\Admin"));
    }

    [Theory]
    [InlineData("CONTOSO\\admin", "$Credsadmin")]
    [InlineData("CONTOSO\\admin-user", "$Credsadmin_user")]
    [InlineData("CONTOSO\\admin.user", "$Credsadmin_user")]
    [InlineData("admin @company", "$Credsadmincompany")]
    [InlineData("admin", "$Credsadmin")]
    [InlineData("ADMIN@CONTOSO.COM", "$CredsADMINCONTOSO_COM")]
    public void ResolveBuildsTheCredentialVariableName(string userName, string expected)
    {
        Assert.Equal(expected, CredentialRepository.Resolve(userName));
    }

    [Fact]
    public void ForReturnsTheSharedStateWhenThereIsNoModule()
    {
        Assert.Same(ModuleState.For(null), ModuleState.For(null));
    }
}
