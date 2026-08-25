using System.Collections;
using Microsoft.Management.Infrastructure;
using Xunit;

namespace ReverseDSC.Tests;

public class DscClassTypeInspectorTests
{
    private readonly ModuleState _state = TestData.NewState();

    [Fact]
    public void RecognizesAnInstanceOfAClassWithTheComplexTypePrefix()
    {
        Assert.True(DscClassTypeInspector.IsDscClassInstance(_state, new MSFT_TestClassMember()));
    }

    [Fact]
    public void RecognizesAnInstanceOfAClassWithADscPropertyButNoPrefix()
    {
        Assert.True(DscClassTypeInspector.IsDscClassInstance(_state, new TestClassWithDscProperty()));
    }

    [Theory]
    [MemberData(nameof(NonClassInstances))]
    public void DoesNotRecognizeOtherValuesAsClassInstances(object? value)
    {
        Assert.False(DscClassTypeInspector.IsDscClassInstance(_state, value));
    }

    [Fact]
    public void DoesNotRecognizeACimInstanceAsAClassInstance()
    {
        CimInstance instance = new("MSFT_TeamMember", TestData.CimNamespace);
        Assert.False(DscClassTypeInspector.IsDscClassInstance(_state, instance));
    }

    [Fact]
    public void RecognizesAStronglyTypedArrayOfClassInstances()
    {
        MSFT_TestClassMember[] value = [new MSFT_TestClassMember()];
        Assert.True(DscClassTypeInspector.IsDscClassInstanceArray(_state, value));
    }

    [Fact]
    public void RecognizesAnEmptyStronglyTypedArray()
    {
        Assert.True(DscClassTypeInspector.IsDscClassInstanceArray(_state, Array.Empty<MSFT_TestClassMember>()));
    }

    [Fact]
    public void RecognizesAnObjectArrayThatHoldsClassInstances()
    {
        object?[] value = [new MSFT_TestClassMember()];
        Assert.True(DscClassTypeInspector.IsDscClassInstanceArray(_state, value));
    }

    [Fact]
    public void SkipsTheLeadingNullElementsOfAnObjectArray()
    {
        object?[] value = [null, new MSFT_TestClassMember()];
        Assert.True(DscClassTypeInspector.IsDscClassInstanceArray(_state, value));
    }

    [Theory]
    [MemberData(nameof(NonClassInstanceArrays))]
    public void DoesNotRecognizeOtherValuesAsClassInstanceArrays(object? value)
    {
        Assert.False(DscClassTypeInspector.IsDscClassInstanceArray(_state, value));
    }

    [Fact]
    public void DoesNotRecognizeAnArrayOfCimInstancesAsAClassInstanceArray()
    {
        CimInstance[] value = [new CimInstance("MSFT_TeamMember", TestData.CimNamespace)];
        Assert.False(DscClassTypeInspector.IsDscClassInstanceArray(_state, value));
    }

    [Fact]
    public void SkipsThePropertiesThatWereNeverSet()
    {
        MSFT_TestClassMember instance = new() { DisplayName = "John Doe" };
        List<DscInstanceProperty> properties = DscClassTypeInspector.GetInstanceProperties(instance);

        Assert.Single(properties);
        Assert.Equal("DisplayName", properties[0].Name);
    }

    [Fact]
    public void ReturnsThePropertiesSortedByName()
    {
        MSFT_TestClassSetting instance = new()
        {
            Name = "Test",
            Enabled = true,
            Tags = ["a"],
            Ports = [80],
        };

        Assert.Equal(
            ["Enabled", "Name", "Ports", "Tags"],
            DscClassTypeInspector.GetInstanceProperties(instance).Select(property => property.Name));
    }

    [Fact]
    public void ReturnsAnEmptyResultForAClassWithoutProperties()
    {
        Assert.Empty(DscClassTypeInspector.GetInstanceProperties(new MSFT_TestClassEmpty()));
    }

    [Fact]
    public void ReturnsTheValueOfANullablePropertyUnwrapped()
    {
        MSFT_TestClassSetting instance = new() { Enabled = true };
        List<DscInstanceProperty> properties = DscClassTypeInspector.GetInstanceProperties(instance);

        Assert.Equal(typeof(bool), properties[0].Value.GetType());
    }

    [Fact]
    public void SkipsThePropertiesThatAreNotDscProperties()
    {
        MSFT_TestClassExtras instance = new() { Name = "Test", Filter = "ExportOnly", Secret = "Hidden" };

        Assert.Equal(
            ["Name"],
            DscClassTypeInspector.GetInstanceProperties(instance).Select(property => property.Name));
    }

    [Fact]
    public void CachesTheVerdictPerType()
    {
        Assert.True(DscClassTypeInspector.IsDscClassType(_state, typeof(MSFT_TestClassMember)));
        Assert.True(_state.DscClassTypes[typeof(MSFT_TestClassMember)]);

        Assert.False(DscClassTypeInspector.IsDscClassType(_state, typeof(TestClassPlain)));
        Assert.False(_state.DscClassTypes[typeof(TestClassPlain)]);
    }

    public static TheoryData<object?> NonClassInstances() =>
    [
        null!,
        "MSFT_NotAClass",
        new Hashtable { { "Key", "Value" } },
        new ArrayList { "a" },
        ConsoleColor.Red,
        new TestClassPlain(),
        new MSFT_TestClassMember[] { new() },
    ];

    public static TheoryData<object?> NonClassInstanceArrays() =>
    [
        null!,
        Array.Empty<object>(),
        new object?[] { null, null },
        new[] { "a", "b" },
        new[] { 1, 2 },
        new ArrayList { "a" },
        new MSFT_TestClassMember(),
    ];
}
