using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using ReverseDSC.Commands;
using Xunit;

namespace ReverseDSC.Tests;

public sealed class CmdletTests : IDisposable
{
    private readonly Runspace _runspace;
    private readonly ModuleState _state = ModuleState.For(null);
    private readonly string _modulePath = Path.Combine(Path.GetTempPath(), $"ReverseDSCCmdlet{Guid.NewGuid():N}.psm1");
    private static readonly string[] NoEscapeNames = ["Name"];

    public CmdletTests()
    {
        _state.Reset();

        InitialSessionState sessionState = InitialSessionState.CreateDefault2();
        sessionState.Commands.Add(
        [
            new SessionStateCmdletEntry("Get-DSCBlock", typeof(GetDscBlockCommand), null),
            new SessionStateCmdletEntry("Get-DSCDependsOnBlock", typeof(GetDscDependsOnBlockCommand), null),
            new SessionStateCmdletEntry("Get-DSCParamType", typeof(GetDscParamTypeCommand), null),
            new SessionStateCmdletEntry("Get-DSCFakeParameters", typeof(GetDscFakeParametersCommand), null),
            new SessionStateCmdletEntry("Convert-DSCStringParamToVariable", typeof(ConvertDscStringParamToVariableCommand), null),
            new SessionStateCmdletEntry("Get-Credentials", typeof(GetCredentialsCommand), null),
            new SessionStateCmdletEntry("Resolve-Credentials", typeof(ResolveCredentialsCommand), null),
            new SessionStateCmdletEntry("Save-Credentials", typeof(SaveCredentialsCommand), null),
            new SessionStateCmdletEntry("Test-Credentials", typeof(TestCredentialsCommand), null),
            new SessionStateCmdletEntry("Add-ReverseDSCUserName", typeof(AddReverseDscUserNameCommand), null),
            new SessionStateCmdletEntry("Clear-ReverseDSCUserNames", typeof(ClearReverseDscUserNamesCommand), null),
            new SessionStateCmdletEntry("Add-ConfigurationDataEntry", typeof(AddConfigurationDataEntryCommand), null),
            new SessionStateCmdletEntry("Get-ConfigurationDataEntry", typeof(GetConfigurationDataEntryCommand), null),
            new SessionStateCmdletEntry("Clear-ConfigurationDataContent", typeof(ClearConfigurationDataContentCommand), null),
            new SessionStateCmdletEntry("Get-ConfigurationDataContent", typeof(GetConfigurationDataContentCommand), null),
            new SessionStateCmdletEntry("New-ConfigurationDataDocument", typeof(NewConfigurationDataDocumentCommand), null),
        ]);

        _runspace = RunspaceFactory.CreateRunspace(sessionState);
        _runspace.Open();

        File.WriteAllText(_modulePath, """
            function Get-TargetResource
            {
                param(
                    [System.String] $Name,
                    [System.Boolean] $Enabled
                )
            }

            function Set-TargetResource
            {
                param(
                    [System.String] $Name,
                    [System.Boolean] $Enabled
                )
            }
            """);
    }

    [Fact]
    public void GetDscBlockRendersTheParameters()
    {
        Hashtable parameters = new() { { "Name", "Test" } };
        Assert.Equal(
            "            Name                 = \"Test\";\r\n",
            InvokeSingle<string>("Get-DSCBlock", new Dictionary<string, object?>
            {
                ["ModulePath"] = _modulePath,
                ["Params"] = parameters,
            }));
    }

    [Fact]
    public void GetDscBlockHonoursNoEscapeAndAllowVariablesInStrings()
    {
        Hashtable parameters = new() { { "Name", "$ConfigName" }, { "Other", "Value of $Node" } };
        string block = InvokeSingle<string>("Get-DSCBlock", new Dictionary<string, object?>
        {
            ["ModulePath"] = _modulePath,
            ["Params"] = parameters,
            ["NoEscape"] = NoEscapeNames,
            ["AllowVariablesInStrings"] = new SwitchParameter(true),
        })!;

        Assert.Contains("Name                 = $ConfigName;", block, StringComparison.Ordinal);
        Assert.Contains("Other                = \"Value of $Node\";", block, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDscDependsOnBlockRendersTheClause()
    {
        Assert.Equal(
            "@(\"[xWebsite]DefaultSite\");",
            InvokeSingle<string>("Get-DSCDependsOnBlock", new Dictionary<string, object?>
            {
                ["DependsOnItems"] = new object[] { "[xWebsite]DefaultSite" },
            }));
    }

    [Fact]
    public void GetDscParamTypeResolvesTheDeclaredType()
    {
        Assert.Equal(
            "System.Boolean",
            InvokeSingle<string>("Get-DSCParamType", new Dictionary<string, object?>
            {
                ["ModulePath"] = _modulePath,
                ["ParamName"] = "$Enabled",
            }));
    }

    [Fact]
    public void GetDscParamTypeWritesNullForAnUnknownParameter()
    {
        Assert.Null(InvokeSingle<string>("Get-DSCParamType", new Dictionary<string, object?>
        {
            ["ModulePath"] = _modulePath,
            ["ParamName"] = "$NonExistent",
        }));
    }

    [Fact]
    public void GetDscFakeParametersReturnsAHashtable()
    {
        Hashtable? parameters = InvokeSingle<Hashtable>("Get-DSCFakeParameters", new Dictionary<string, object?>
        {
            ["ModulePath"] = _modulePath,
        });

        Assert.NotNull(parameters);
        Assert.Equal("*", parameters!["Name"]);
        Assert.Equal(true, parameters["Enabled"]);
    }

    [Fact]
    public void ConvertDscStringParamToVariableRemovesTheQuotes()
    {
        Assert.Equal(
            "            ParamName            = SomeValue;\r\n",
            InvokeSingle<string>("Convert-DSCStringParamToVariable", new Dictionary<string, object?>
            {
                ["DSCBlock"] = "            ParamName            = \"SomeValue\";\r\n",
                ["ParameterName"] = "ParamName",
            }));
    }

    [Fact]
    public void ConvertDscStringParamToVariableAcceptsTheCimSwitches()
    {
        Assert.Equal(
            "            Content = <Rule Name=\"Block\" />;\r\n",
            InvokeSingle<string>("Convert-DSCStringParamToVariable", new Dictionary<string, object?>
            {
                ["DSCBlock"] = "            Content = \"<Rule Name=`\"Block`\" />\";\r\n",
                ["ParameterName"] = "Content",
                ["IsCIMObject"] = true,
            }));
    }

    [Fact]
    public void TheCredentialCmdletsShareTheModuleState()
    {
        Assert.False(InvokeSingle<bool>("Test-Credentials", Argument("UserName", "CONTOSO\\admin")));
        Assert.Null(InvokeSingle<string>("Get-Credentials", Argument("UserName", "CONTOSO\\admin")));

        Invoke("Save-Credentials", Argument("UserName", "CONTOSO\\ADMIN"));

        Assert.True(InvokeSingle<bool>("Test-Credentials", Argument("UserName", "CONTOSO\\admin")));
        Assert.Equal("contoso\\admin", InvokeSingle<string>("Get-Credentials", Argument("UserName", "CONTOSO\\Admin")));
    }

    [Fact]
    public void ResolveCredentialsBuildsTheVariableName()
    {
        Assert.Equal(
            "$Credssvc_admin",
            InvokeSingle<string>("Resolve-Credentials", Argument("UserName", "CONTOSO\\svc-admin")));
    }

    [Fact]
    public void TheUserNameCmdletsAccumulateAndClear()
    {
        Invoke("Add-ReverseDSCUserName", Argument("UserName", "user1@contoso.com"));
        Invoke("Add-ReverseDSCUserName", Argument("UserName", "user1@contoso.com"));
        Assert.Single(_state.UserNames);

        Invoke("Clear-ReverseDSCUserNames", []);
        Assert.Empty(_state.UserNames);
    }

    [Fact]
    public void TheConfigurationDataCmdletsRoundTripAnEntry()
    {
        Invoke("Add-ConfigurationDataEntry", new Dictionary<string, object?>
        {
            ["Node"] = "localhost",
            ["Key"] = "TenantId",
            ["Value"] = "contoso.onmicrosoft.com",
            ["Description"] = "The tenant",
        });

        Hashtable? entry = InvokeSingle<Hashtable>("Get-ConfigurationDataEntry", new Dictionary<string, object?>
        {
            ["Node"] = "localhost",
            ["Key"] = "TenantId",
        });

        Assert.NotNull(entry);
        Assert.Equal("contoso.onmicrosoft.com", entry!["Value"]);
        Assert.Equal("The tenant", entry["Description"]);

        string content = InvokeSingle<string>("Get-ConfigurationDataContent", [])!;
        Assert.Contains("NodeName                    = \"localhost\"", content, StringComparison.Ordinal);
        Assert.Contains("# The tenant", content, StringComparison.Ordinal);

        Invoke("Clear-ConfigurationDataContent", []);
        Assert.Null(InvokeSingle<Hashtable>("Get-ConfigurationDataEntry", new Dictionary<string, object?>
        {
            ["Node"] = "localhost",
            ["Key"] = "TenantId",
        }));
    }

    [Fact]
    public void GetConfigurationDataEntrySearchesEveryNodeWhenNoneIsGiven()
    {
        Invoke("Add-ConfigurationDataEntry", new Dictionary<string, object?>
        {
            ["Node"] = "NonNodeData",
            ["Key"] = "ApplicationId",
            ["Value"] = "12345678",
        });

        Hashtable? entry = InvokeSingle<Hashtable>("Get-ConfigurationDataEntry", Argument("Key", "ApplicationId"));

        Assert.NotNull(entry);
        Assert.Equal("12345678", entry!["Value"]);
    }

    [Fact]
    public void NewConfigurationDataDocumentWritesUtf8WithoutABom()
    {
        string documentPath = Path.Combine(Path.GetTempPath(), $"ReverseDSCData{Guid.NewGuid():N}.psd1");
        Invoke("Add-ConfigurationDataEntry", new Dictionary<string, object?>
        {
            ["Node"] = "localhost",
            ["Key"] = "TenantId",
            ["Value"] = "contoso",
        });

        try
        {
            Invoke("New-ConfigurationDataDocument", Argument("Path", documentPath));

            byte[] bytes = File.ReadAllBytes(documentPath);
            Assert.Equal((byte)'@', bytes[0]);
            Assert.Equal((byte)'{', bytes[1]);
            Assert.Equal([(byte)'}', (byte)'\r', (byte)'\n'], bytes[^3..]);
        }
        finally
        {
            File.Delete(documentPath);
        }
    }

    [Fact]
    public void GetConfigurationDataContentWarnsAboutAValueItCannotConvert()
    {
        Invoke("Add-ConfigurationDataEntry", new Dictionary<string, object?>
        {
            ["Node"] = "NonNodeData",
            ["Key"] = "Count",
            ["Value"] = 5,
        });

        using PowerShell shell = PowerShell.Create();
        shell.Runspace = _runspace;
        shell.AddCommand("Get-ConfigurationDataContent");
        Collection<PSObject> output = shell.Invoke();

        Assert.DoesNotContain("Count =", output[0].BaseObject.ToString(), StringComparison.Ordinal);
        Assert.Contains(shell.Streams.Warning, record => record.Message.Contains("Could not obtain value for key Count", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryModuleGetsItsOwnStateAndKeepsIt()
    {
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = _runspace;
        shell.AddCommand("New-Module").AddParameter("Name", "Probe").AddParameter("ScriptBlock", ScriptBlock.Create(string.Empty));
        PSModuleInfo module = (PSModuleInfo)shell.Invoke()[0].BaseObject;

        ModuleState moduleState = ModuleState.For(module);

        Assert.Same(moduleState, ModuleState.For(module));
        Assert.NotSame(moduleState, ModuleState.For(null));
    }

    public void Dispose()
    {
        _runspace.Dispose();
        _state.Reset();
        if (File.Exists(_modulePath))
        {
            File.Delete(_modulePath);
        }
    }

    private static Dictionary<string, object?> Argument(string name, object? value)
    {
        return new Dictionary<string, object?> { [name] = value };
    }

    private Collection<PSObject> Invoke(string command, Dictionary<string, object?> parameters)
    {
        using PowerShell shell = PowerShell.Create();
        shell.Runspace = _runspace;
        shell.AddCommand(command);
        foreach (KeyValuePair<string, object?> parameter in parameters)
        {
            shell.AddParameter(parameter.Key, parameter.Value);
        }

        Collection<PSObject> output = shell.Invoke();
        Assert.Empty(shell.Streams.Error);
        return output;
    }

    private T? InvokeSingle<T>(string command, Dictionary<string, object?> parameters)
    {
        Collection<PSObject> output = Invoke(command, parameters);
        Assert.Single(output);
        return output[0] is null ? default : (T?)output[0].BaseObject;
    }
}
