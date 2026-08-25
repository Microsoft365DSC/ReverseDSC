using System.Management.Automation.Language;
using Xunit;

namespace ReverseDSC.Tests;

public class ModuleStateTests : IDisposable
{
    private readonly ModuleState _state = TestData.NewState();
    private readonly string _modulePath = Path.Combine(Path.GetTempPath(), $"ReverseDSCAst{Guid.NewGuid():N}.psm1");

    [Fact]
    public void ParsesTheModuleFileIntoAScriptBlockAst()
    {
        WriteModule("function Get-TargetResource { param([System.String] $Name) }");

        Assert.IsType<ScriptBlockAst>(ModuleAstCache.GetModuleAst(_state, _modulePath));
    }

    [Fact]
    public void ReturnsTheCachedAstOnSubsequentCalls()
    {
        WriteModule("function Get-TargetResource { param([System.String] $Name) }");
        ScriptBlockAst first = ModuleAstCache.GetModuleAst(_state, _modulePath);

        WriteModule("function Get-Something { }");
        ScriptBlockAst second = ModuleAstCache.GetModuleAst(_state, _modulePath);

        Assert.Same(first, second);
        Assert.True(_state.ModuleAstCache.ContainsKey(_modulePath));
    }

    [Fact]
    public void StoresAUserNameOnlyOnce()
    {
        UserNameRepository.Add(_state, "user1@contoso.com");
        UserNameRepository.Add(_state, "user2@contoso.com");
        UserNameRepository.Add(_state, "user1@contoso.com");

        Assert.Equal(["user1@contoso.com", "user2@contoso.com"], UserNameRepository.GetAll(_state));
    }

    [Fact]
    public void EmptiesTheUserNameListWhenCleared()
    {
        UserNameRepository.Add(_state, "user1@contoso.com");
        UserNameRepository.Clear(_state);

        Assert.Empty(UserNameRepository.GetAll(_state));
    }

    [Fact]
    public void StoresACredentialOnlyOnceRegardlessOfCasing()
    {
        CredentialRepository.Save(_state, "CONTOSO\\admin");
        CredentialRepository.Save(_state, "contoso\\ADMIN");

        Assert.Single(_state.Credentials);
    }

    [Fact]
    public void KeepsSeparateEntriesForDifferentUsers()
    {
        CredentialRepository.Save(_state, "CONTOSO\\admin");
        CredentialRepository.Save(_state, "CONTOSO\\reader");

        Assert.Equal(2, _state.Credentials.Count);
    }

    [Fact]
    public void ResetsEveryPieceOfAccumulatedState()
    {
        CredentialRepository.Save(_state, "CONTOSO\\admin");
        UserNameRepository.Add(_state, "admin@contoso.com");
        ConfigurationDataStore.AddEntry(_state, "localhost", "TenantId", "contoso", null);
        DscClassTypeInspector.IsDscClassType(_state, typeof(MSFT_TestClassMember));

        _state.Reset();

        Assert.Empty(_state.Credentials);
        Assert.Empty(_state.UserNames);
        Assert.Empty(_state.DscClassTypes);
        Assert.Empty(_state.ModuleAstCache);
        Assert.Null(ConfigurationDataStore.GetEntry(_state, "localhost", "TenantId"));
    }

    [Fact]
    public void KeepsTheStateOfEveryModuleSeparate()
    {
        ModuleState other = TestData.NewState();
        CredentialRepository.Save(_state, "CONTOSO\\admin");

        Assert.True(CredentialRepository.Test(_state, "CONTOSO\\admin"));
        Assert.False(CredentialRepository.Test(other, "CONTOSO\\admin"));
    }

    public void Dispose()
    {
        if (File.Exists(_modulePath))
        {
            File.Delete(_modulePath);
        }

        GC.SuppressFinalize(this);
    }

    private void WriteModule(string content)
    {
        File.WriteAllText(_modulePath, content);
    }
}
