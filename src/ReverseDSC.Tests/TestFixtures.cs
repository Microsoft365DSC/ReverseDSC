using System.Collections.Specialized;
using System.Management.Automation;
using System.Security;
using Microsoft.Management.Infrastructure;

namespace ReverseDSC.Tests;

public class MSFT_TestClassMember
{
    [DscProperty] public string? DisplayName { get; set; }

    [DscProperty] public string? Role { get; set; }
}

public class MSFT_TestClassEmpty
{
}

public class MSFT_TestClassSetting
{
    [DscProperty] public string? Name { get; set; }

    [DscProperty] public bool? Enabled { get; set; }

    [DscProperty] public int[]? Ports { get; set; }

    [DscProperty] public string[]? Tags { get; set; }

    [DscProperty] public int? Missing { get; set; }
}

public class MSFT_TestClassTeam
{
    [DscProperty] public string? DisplayName { get; set; }

    [DscProperty] public MSFT_TestClassMember? Owner { get; set; }

    [DscProperty] public MSFT_TestClassMember[]? Members { get; set; }
}

public class MSFT_TestClassIntuneAssignment
{
    [DscProperty] public string? GroupId { get; set; }
}

public class MSFT_TestClassEnumHolder
{
    [DscProperty] public ConsoleColor? Color { get; set; }
}

public class MSFT_TestClassExtras
{
    [DscProperty] public string? Name { get; set; }

    public string? Filter { get; set; }

    public string? Secret { get; set; }
}

public class TestClassWithDscProperty
{
    [DscProperty] public string? Name { get; set; }
}

public class TestClassPlain
{
    public string? Name { get; set; }
}

internal static class TestData
{
    internal const string CimNamespace = "root/microsoft/windows/desiredstateconfiguration";

    internal static ModuleState NewState() => new();

    internal static MSFT_TestClassMember John() => new() { DisplayName = "John Doe", Role = "Owner" };

    internal static MSFT_TestClassMember Jane() => new() { DisplayName = "Jane Roe", Role = "Member" };

    internal static CimInstance NewCimInstance(string className, OrderedDictionary properties)
    {
        CimInstance instance = new(className, CimNamespace);
        foreach (object key in properties.Keys)
        {
            instance.CimInstanceProperties.Add(CimProperty.Create((string)key, properties[key], CimFlags.Property));
        }

        return instance;
    }

    internal static CimInstance CimJohn() => NewCimInstance(
        "MSFT_TeamMember",
        new OrderedDictionary { { "DisplayName", "John Doe" }, { "Role", "Owner" } });

    internal static CimInstance CimJane() => NewCimInstance(
        "MSFT_TeamMember",
        new OrderedDictionary { { "DisplayName", "Jane Roe" }, { "Role", "Member" } });

    internal static string Lines(params string[] lines) => string.Join("\r\n", lines);

    internal static PSCredential Credential(string userName)
    {
        SecureString password = new();
        foreach (char character in "Password123")
        {
            password.AppendChar(character);
        }

        return new PSCredential(userName, password);
    }
}
