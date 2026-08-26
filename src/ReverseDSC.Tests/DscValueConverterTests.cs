using System.Collections;
using Xunit;

namespace ReverseDSC.Tests;

public class DscValueConverterTests
{
    [Fact]
    public void ConvertStringReturnsEmptyQuotedStringForNull()
    {
        Assert.Equal("\"\"", DscValueConverter.ConvertString(null, false, false));
    }

    [Fact]
    public void ConvertStringReturnsRawValueWhenNotEscaping()
    {
        Assert.Equal("MyValue", DscValueConverter.ConvertString("MyValue", true, false));
    }

    [Fact]
    public void ConvertStringWrapsValueInDoubleQuotes()
    {
        Assert.Equal("\"SimpleString\"", DscValueConverter.ConvertString("SimpleString", false, false));
    }

    [Fact]
    public void ConvertStringEscapesSpecialCharacters()
    {
        Assert.Equal("\"Value with `$var\"", DscValueConverter.ConvertString("Value with $var", false, false));
    }

    [Fact]
    public void ConvertStringPreservesDollarSignsWhenVariablesAreAllowed()
    {
        Assert.Equal("\"Value with $var\"", DscValueConverter.ConvertString("Value with $var", false, true));
    }

    [Theory]
    [InlineData(true, "$True")]
    [InlineData(false, "$False")]
    public void ConvertBooleanUsesTheDollarPrefixedForm(bool value, string expected)
    {
        Assert.Equal(expected, DscValueConverter.ConvertBoolean(value));
    }

    [Theory]
    [InlineData("admin", "$Credsadmin")]
    [InlineData("admin-user", "$Credsadmin_user")]
    [InlineData("admin.user", "$Credsadmin_user")]
    [InlineData("admin user", "$Credsadminuser")]
    [InlineData("admin@contoso.com", "$Credsadmincontoso_com")]
    public void FormatCredentialVariableNameStripsInvalidCharacters(string userName, string expected)
    {
        Assert.Equal(expected, DscValueConverter.FormatCredentialVariableName(userName));
    }

    [Fact]
    public void ConvertCredentialFallsBackToGetCredentialForNull()
    {
        Assert.Equal("Get-Credential -Message Credential", DscValueConverter.ConvertCredential(null, "Credential"));
    }

    [Theory]
    [InlineData("admin@contoso.com", "$Credsadmin")]
    [InlineData("CONTOSO\\svc-admin", "$Credssvc_admin")]
    [InlineData("admin.user", "$Credsadmin_user")]
    [InlineData("CONTOSO\\admin@contoso.com", "$Credsadmincontoso_com")]
    public void ConvertCredentialStripsTheDomain(string userName, string expected)
    {
        Assert.Equal(expected, DscValueConverter.ConvertCredential(TestData.Credential(userName), "Credential"));
    }

    [Fact]
    public void ConvertHashtableRendersASingleLineHashtable()
    {
        Hashtable value = new() { { "SubKey", "SubValue" } };
        Assert.Equal("@{SubKey = \"SubValue\"; }", DscValueConverter.ConvertHashtable(value));
    }

    [Fact]
    public void ConvertHashtableRendersAnEmptyHashtable()
    {
        Assert.Equal("@{}", DscValueConverter.ConvertHashtable(new Hashtable()));
    }

    [Fact]
    public void ConvertHashtableQuotesNonStringValues()
    {
        Hashtable value = new() { { "Count", 5 } };
        Assert.Equal("@{Count = \"5\"; }", DscValueConverter.ConvertHashtable(value));
    }

    [Theory]
    [MemberData(nameof(EmptyArrays))]
    public void ConvertStringArrayReturnsAnEmptyArray(object?[]? value)
    {
        Assert.Equal("@()", DscValueConverter.ConvertStringArray(value, false, false));
    }

    [Fact]
    public void ConvertStringArrayRendersASingleElement()
    {
        Assert.Equal("@(\"Item1\")", DscValueConverter.ConvertStringArray(["Item1"], false, false));
    }

    [Fact]
    public void ConvertStringArrayRendersMultipleElements()
    {
        Assert.Equal("@(\"Item1\",\"Item2\")", DscValueConverter.ConvertStringArray(["Item1", "Item2"], false, false));
    }

    [Fact]
    public void ConvertStringArraySkipsNullElements()
    {
        Assert.Equal("@(\"Item1\",\"Item2\")", DscValueConverter.ConvertStringArray(["Item1", null, "Item2"], false, false));
    }

    [Fact]
    public void ConvertStringArrayEscapesTheElements()
    {
        Assert.Equal("@(\"a `\"b`\"\",\"c `$d\")", DscValueConverter.ConvertStringArray(["a \"b\"", "c $d"], false, false));
    }

    [Fact]
    public void ConvertStringArrayLeavesTheElementsUnescapedWhenAsked()
    {
        Assert.Equal("@(\"a \"b\"\")", DscValueConverter.ConvertStringArray(["a \"b\""], true, false));
    }

    [Fact]
    public void ConvertStringArrayPreservesVariablesWhenAsked()
    {
        Assert.Equal("@(\"$Node\")", DscValueConverter.ConvertStringArray(["$Node"], false, true));
    }

    [Theory]
    [MemberData(nameof(EmptyArrays))]
    public void ConvertIntegerArrayReturnsAnEmptyArray(object?[]? value)
    {
        Assert.Equal("@()", DscValueConverter.ConvertIntegerArray(value));
    }

    [Fact]
    public void ConvertIntegerArrayRendersTheValuesWithoutQuotes()
    {
        Assert.Equal("@(80,443)", DscValueConverter.ConvertIntegerArray([80, 443]));
    }

    [Fact]
    public void ConvertIntegerArrayRendersASingleValue()
    {
        Assert.Equal("@(80)", DscValueConverter.ConvertIntegerArray([80]));
    }

    [Theory]
    [MemberData(nameof(EmptyArrays))]
    public void ConvertObjectArrayReturnsAnEmptyArray(object?[]? value)
    {
        Assert.Equal("@()", DscValueConverter.ConvertObjectArray(value, false, false));
    }

    [Fact]
    public void ConvertObjectArrayQuotesAndEscapesStrings()
    {
        Assert.Equal("@(\"x\",\"y\")", DscValueConverter.ConvertObjectArray(["x", "y"], false, false));
        Assert.Equal("@(\"a `\"b`\"\")", DscValueConverter.ConvertObjectArray(["a \"b\""], false, false));
    }

    [Fact]
    public void ConvertObjectArrayConcatenatesStringsWhenNotEscaping()
    {
        Assert.Equal("@(ab)", DscValueConverter.ConvertObjectArray(["a", "b"], true, false));
    }

    [Fact]
    public void ConvertObjectArrayTrimsASingleTrailingCommaWhenNotEscaping()
    {
        Assert.Equal("@(a,b)", DscValueConverter.ConvertObjectArray(["a,", "b,"], true, false));
    }

    [Fact]
    public void ConvertObjectArrayPreservesVariablesWhenAsked()
    {
        Assert.Equal("@(\"$Node\")", DscValueConverter.ConvertObjectArray(["$Node"], false, true));
    }

    [Fact]
    public void ConvertObjectArrayRendersHashtableElements()
    {
        Hashtable first = new() { { "A", "B" } };
        Assert.Equal("@(@{A='B'})", DscValueConverter.ConvertObjectArray([first], false, false));
    }

    private static readonly string[] HashtableArrayValue = ["p", "q"];

    [Fact]
    public void ConvertObjectArrayRendersArrayValuesOfHashtableElements()
    {
        Hashtable first = new() { { "A", HashtableArrayValue } };
        Assert.Equal("@(@{A=@('p', 'q')})", DscValueConverter.ConvertObjectArray([first], false, false));
    }

    [Fact]
    public void ConvertObjectArrayRendersNullValuesOfHashtableElements()
    {
        Hashtable first = new() { { "A", null } };
        Assert.Equal("@(@{A=$null})", DscValueConverter.ConvertObjectArray([first], false, false));
    }

    [Fact]
    public void ConvertObjectArrayConcatenatesHashtableElementsWithoutASeparator()
    {
        Hashtable first = new() { { "A", "B" } };
        Hashtable second = new() { { "C", "D" } };
        Assert.Equal("@(@{A='B'}@{C='D'})", DscValueConverter.ConvertObjectArray([first, second], false, false));
    }

    [Fact]
    public void ConvertObjectArrayConcatenatesOtherElements()
    {
        Assert.Equal("@(12)", DscValueConverter.ConvertObjectArray([1, 2], false, false));
    }

    [Fact]
    public void ConvertValueQuotesEnumValues()
    {
        ModuleState state = TestData.NewState();
        Assert.Equal("\"Red\"", DscValueConverter.ConvertValue(state, ConsoleColor.Red, "Color", 12, false, false));
    }

    [Fact]
    public void ConvertValueRendersNumbersWithoutQuotes()
    {
        ModuleState state = TestData.NewState();
        Assert.Equal("8080", DscValueConverter.ConvertValue(state, 8080, "Port", 12, false, false));
    }

    [Fact]
    public void ConvertValueRendersDatesWithTheInvariantCulture()
    {
        ModuleState state = TestData.NewState();
        DateTime moment = new(2026, 8, 25, 13, 45, 56, DateTimeKind.Utc);
        Assert.Equal("\"08/25/2026 13:45:56\"", DscValueConverter.ConvertValue(state, moment, "Moment", 12, false, false));
    }

    [Fact]
    public void ConvertValueRendersGenericStringListsAsStringArrays()
    {
        ModuleState state = TestData.NewState();
        List<string> value = ["A", "B"];
        Assert.Equal("@(\"A\",\"B\")", DscValueConverter.ConvertValue(state, value, "Items", 12, false, false));
    }

    [Fact]
    public void ConvertValueRendersArrayListsAsStringArrays()
    {
        ModuleState state = TestData.NewState();
        ArrayList value = ["A", "B"];
        Assert.Equal("@(\"A\",\"B\")", DscValueConverter.ConvertValue(state, value, "Items", 12, false, false));
    }

    public static TheoryData<object?[]?> EmptyArrays() =>
    [
        null!,
        Array.Empty<object?>(),
        new object?[] { null },
    ];
}
