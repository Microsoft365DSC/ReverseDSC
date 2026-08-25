class MSFT_TestClassMember
{
    [DscProperty()]
    [System.String] $DisplayName

    [DscProperty()]
    [System.String] $Role
}

class MSFT_TestClassEmpty
{
}

class MSFT_TestClassSetting
{
    [DscProperty()]
    [System.String] $Name

    [DscProperty()]
    [System.Nullable[System.Boolean]] $Enabled

    [DscProperty()]
    [System.Int32[]] $Ports

    [DscProperty()]
    [System.String[]] $Tags

    [DscProperty()]
    [System.Nullable[System.Int32]] $Missing
}

class MSFT_TestClassTeam
{
    [DscProperty()]
    [System.String] $DisplayName

    [DscProperty()]
    [MSFT_TestClassMember] $Owner

    [DscProperty()]
    [MSFT_TestClassMember[]] $Members
}

# The name has to contain "int" to hit the 'Int.*\[\]' case of ConvertTo-DSCValue.
class MSFT_TestClassIntuneAssignment
{
    [DscProperty()]
    [System.String] $GroupId
}

class MSFT_TestClassEnumHolder
{
    [DscProperty()]
    [System.Nullable[System.ConsoleColor]] $Color
}

# Only the DSC properties are rendered.
class MSFT_TestClassExtras
{
    [DscProperty()]
    [System.String] $Name

    [System.String] $Filter

    hidden [System.String] $Secret
}

# Recognized by the [DscProperty()] attribute alone, without the MSFT_ prefix.
class TestClassWithDscProperty
{
    [DscProperty()]
    [System.String] $Name
}

# Neither the prefix nor the attribute, so this is not a complex type.
class TestClassPlain
{
    [System.String] $Name
}

$modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\ReverseDSC\ReverseDSC.psd1'
Import-Module -Name $modulePath -Force

BeforeAll {
    function New-TestCimInstance
    {
        param(
            [Parameter(Mandatory = $true)]
            [System.String]
            $ClassName,

            [Parameter(Mandatory = $true)]
            [System.Collections.Specialized.OrderedDictionary]
            $Properties
        )

        $instance = [Microsoft.Management.Infrastructure.CimInstance]::new($ClassName, 'root/microsoft/windows/desiredstateconfiguration')
        foreach ($name in $Properties.Keys)
        {
            $property = [Microsoft.Management.Infrastructure.CimProperty]::Create(
                $name,
                $Properties[$name],
                [Microsoft.Management.Infrastructure.CimFlags]::Property)
            $instance.CimInstanceProperties.Add($property)
        }
        return $instance
    }
}

Describe 'Get-DSCDependsOnBlock' {
    It 'Should generate a proper DependsOn clause for a single dependency' {
        $result = Get-DSCDependsOnBlock -DependsOnItems @('[xWebsite]DefaultSite')
        $result | Should -Be '@("[xWebsite]DefaultSite");'
    }

    It 'Should generate a proper DependsOn clause for multiple dependencies' {
        $result = Get-DSCDependsOnBlock -DependsOnItems @('[xWebsite]DefaultSite', '[xSPSite]MainSite')
        $result | Should -Be '@("[xWebsite]DefaultSite","[xSPSite]MainSite");'
    }
}

Describe 'Credential repository' {
    It 'Should report an unknown user as not stored' {
        Test-Credentials -UserName 'CONTOSO\never-saved' | Should -BeFalse
        Get-Credentials -UserName 'CONTOSO\never-saved' | Should -BeNullOrEmpty
    }

    It 'Should store a username in lowercase and report it as stored' {
        Save-Credentials -UserName 'CONTOSO\ADMIN'
        Test-Credentials -UserName 'CONTOSO\admin' | Should -BeTrue
        Get-Credentials -UserName 'CONTOSO\Admin' | Should -Be 'contoso\admin'
    }
}

Describe 'Resolve-Credentials' {
    It 'Should resolve <UserName> to <Expected>' -ForEach @(
        @{ UserName = 'CONTOSO\admin'; Expected = '$Credsadmin' }
        @{ UserName = 'CONTOSO\admin-user'; Expected = '$Credsadmin_user' }
        @{ UserName = 'CONTOSO\admin.user'; Expected = '$Credsadmin_user' }
        @{ UserName = 'admin @company'; Expected = '$Credsadmincompany' }
        @{ UserName = 'admin'; Expected = '$Credsadmin' }
        @{ UserName = 'ADMIN@CONTOSO.COM'; Expected = '$CredsADMINCONTOSO_COM' }
    ) {
        Resolve-Credentials -UserName $UserName | Should -Be $Expected
    }
}

Describe 'Add-ConfigurationDataEntry' {
    BeforeEach {
        Clear-ConfigurationDataContent
    }

    It 'Should add an entry under a new node' {
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1' -Value 'Value1'
        $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1'
        $result.Value | Should -Be 'Value1'
    }

    It 'Should add an entry with a description' {
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1' -Value 'Value1' -Description 'Test setting'
        $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1'
        $result.Value | Should -Be 'Value1'
        $result.Description | Should -Be 'Test setting'
    }

    It 'Should update the value when adding the same key to the same node' {
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1' -Value 'Value1'
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1' -Value 'Value2'
        $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'Setting1'
        $result.Value | Should -Be 'Value2'
    }

    It 'Should support multiple nodes' {
        Add-ConfigurationDataEntry -Node 'Server1' -Key 'Key1' -Value 'A'
        Add-ConfigurationDataEntry -Node 'Server2' -Key 'Key1' -Value 'B'
        (Get-ConfigurationDataEntry -Node 'Server1' -Key 'Key1').Value | Should -Be 'A'
        (Get-ConfigurationDataEntry -Node 'Server2' -Key 'Key1').Value | Should -Be 'B'
    }
}

Describe 'Get-ConfigurationDataEntry' {
    BeforeEach {
        Clear-ConfigurationDataContent
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey' -Value 'TestValue'
        Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'Thumbprint' -Value 'abc123'
    }

    Context 'When the node is specified' {
        It 'Should return the entry for a specific node and key' {
            $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey'
            $result | Should -Not -BeNullOrEmpty
            $result.Value | Should -Be 'TestValue'
        }

        It 'Should return null when the key does not exist' {
            Get-ConfigurationDataEntry -Node 'localhost' -Key 'NonExistent' | Should -BeNullOrEmpty
        }

        It 'Should return null when the node does not exist' {
            Get-ConfigurationDataEntry -Node 'UnknownNode' -Key 'TestKey' | Should -BeNullOrEmpty
        }

        It 'Should not return an entry that only exists under another node' {
            Get-ConfigurationDataEntry -Node 'localhost' -Key 'Thumbprint' | Should -BeNullOrEmpty
        }
    }

    Context 'When the node is omitted' {
        It 'Should search every node and return the match' {
            (Get-ConfigurationDataEntry -Key 'TestKey').Value | Should -Be 'TestValue'
        }

        It 'Should also find entries stored under NonNodeData' {
            (Get-ConfigurationDataEntry -Key 'Thumbprint').Value | Should -Be 'abc123'
        }

        It 'Should return null when no node holds the key' {
            Get-ConfigurationDataEntry -Key 'NonExistent' | Should -BeNullOrEmpty
        }

        It 'Should treat an empty node name the same as an omitted one' {
            (Get-ConfigurationDataEntry -Node '' -Key 'Thumbprint').Value | Should -Be 'abc123'
        }
    }
}

Describe 'Clear-ConfigurationDataContent' {
    It 'Should clear all configuration data entries' {
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey' -Value 'TestValue'
        Clear-ConfigurationDataContent
        $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey'
        $result | Should -BeNullOrEmpty
    }
}

Describe 'Get-ConfigurationDataContent' {
    BeforeEach {
        Clear-ConfigurationDataContent
    }

    Context 'When a node has a documented entry' {
        BeforeEach {
            Add-ConfigurationDataEntry -Node 'localhost' -Key 'ServerName' -Value 'MyServer' -Description 'The server name'
        }

        It 'Should wrap the content in a hashtable with an AllNodes and a NonNodeData section' {
            $result = Get-ConfigurationDataContent
            $result | Should -Match '^@\{'
            $result | Should -Match 'AllNodes'
            $result | Should -Match 'NonNodeData'
            $result | Should -Match '\}$'
        }

        It 'Should include the node name and the DSC credential settings' {
            $result = Get-ConfigurationDataContent
            $result | Should -Match 'NodeName\s+= "localhost"'
            $result | Should -Match 'PSDscAllowPlainTextPassword = \$true;'
            $result | Should -Match 'PSDscAllowDomainUser        = \$true;'
        }

        It 'Should include the key, value and description' {
            $result = Get-ConfigurationDataContent
            $result | Should -Match '# The server name'
            $result | Should -Match 'ServerName = "MyServer"'
        }
    }

    Context 'When node values start with @( or a variable prefix' {
        It 'Should emit array and variable values unquoted' {
            Add-ConfigurationDataEntry -Node 'node1' -Key 'Feat' -Value '@("a","b")'
            Add-ConfigurationDataEntry -Node 'node1' -Key 'Var' -Value '$data'
            $result = Get-ConfigurationDataContent
            $result | Should -Match 'Feat = @\("a","b"\)'
            $result | Should -Match 'Var = \$data'
        }
    }

    Context 'When node values are object arrays' {
        It 'Should emit the array via ConvertTo-ConfigurationDataString' {
            Add-ConfigurationDataEntry -Node 'node1' -Key 'Arr' -Value @('x', 'y')
            $result = Get-ConfigurationDataContent
            $result | Should -Match '"x";'
            $result | Should -Match '"y";'
        }
    }

    Context 'When entries have no description' {
        It 'Should not emit an empty comment line' {
            Add-ConfigurationDataEntry -Node 'localhost' -Key 'Undocumented' -Value 'Value'
            Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'AlsoUndocumented' -Value 'Value'
            $result = Get-ConfigurationDataContent
            ($result -split "`r`n" | Where-Object { $_.Trim() -eq '#' }) | Should -BeNullOrEmpty
        }
    }

    Context 'When multiple nodes are present' {
        It 'Should separate the nodes with a comma and not leave a trailing one' {
            Add-ConfigurationDataEntry -Node 'Server1' -Key 'Key1' -Value 'A'
            Add-ConfigurationDataEntry -Node 'Server2' -Key 'Key1' -Value 'B'
            $result = Get-ConfigurationDataContent
            ([regex]::Matches($result, '\},\r\n')) | Should -HaveCount 1
            $result | Should -Not -Match '\},\r\n    \)'
        }
    }

    Context 'When NonNodeData entries are present' {
        It 'Should include the NonNodeData section with string values' {
            Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'Thumbprint' -Value 'abc123' -Description 'cert thumbprint'
            $result = Get-ConfigurationDataContent
            $result | Should -Match '# cert thumbprint'
            $result | Should -Match 'Thumbprint = "abc123"'
        }

        It 'Should emit object arrays in NonNodeData as quoted arrays' {
            Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'Servers' -Value @('s1', 's2')
            $result = Get-ConfigurationDataContent
            $result | Should -Match '@\("s1","s2"\)'
        }

        It 'Should emit array strings in NonNodeData unquoted' {
            Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'RawList' -Value '@("a","b")'
            $result = Get-ConfigurationDataContent
            $result | Should -Match 'RawList = @\("a","b"\)'
        }
    }
}

Describe 'Convert-DSCStringParamToVariable' {
    Context 'When converting a simple string parameter to a variable' {
        It 'Should remove the quotes around the parameter value' {
            $dscBlock = "            ParamName            = `"SomeValue`";`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Be "            ParamName            = SomeValue;`r`n"
        }
    }

    Context 'When the DSC block has no terminating line break' {
        It 'Should remove the quotes around the parameter value' {
            $dscBlock = '            ParamName            = "SomeValue"'
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Be '            ParamName            = SomeValue'
        }
    }

    Context 'When the parameter name is not found' {
        It 'Should return the original DSCBlock unchanged' {
            $dscBlock = "            OtherParam           = `"Value`";`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'NonExistent'
            $result | Should -Be $dscBlock
        }
    }

    Context 'When other parameters follow the target parameter' {
        It 'Should only unquote the target parameter value' {
            $dscBlock = "            ParamName = `"Value`";`r`n            Other = `"X`";`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Be "            ParamName = Value;`r`n            Other = `"X`";`r`n"
        }
    }

    Context 'When the value has no closing quote' {
        It 'Should return the DSC block unchanged instead of failing' {
            $dscBlock = "            ParamName            = `"SomeValue;`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Be $dscBlock
        }
    }

    Context 'When the value is closed by a single quote instead of a double quote' {
        It 'Should fall back to the single quote as the closing delimiter' {
            $dscBlock = "            ParamName            = `"SomeValue';`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Be "            ParamName            = SomeValue;`r`n"
        }
    }

    Context 'When converting a CIM instance array parameter' {
        BeforeAll {
            $dscBlock = @(
                '            Members              = "@(MSFT_TeamMember{'
                '                DisplayName = `"John Doe`"'
                '                Role        = `"Owner`"'
                '            }'
                '            MSFT_TeamMember{'
                '                DisplayName = `"Jane Roe`"'
                '                Role        = `"Member`"'
                '            })";'
                ''
            ) -join "`r`n"
        }

        It 'Should remove the surrounding quotes and unescape the inner quotes' {
            $expected = @(
                '            Members              = @(MSFT_TeamMember{'
                '                DisplayName = "John Doe"'
                '                Role        = "Owner"'
                '            }'
                '            MSFT_TeamMember{'
                '                DisplayName = "Jane Roe"'
                '                Role        = "Member"'
                '            });'
                ''
            ) -join "`r`n"

            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Members' -IsCIMArray $true
            $result | Should -Be $expected
        }
    }

    Context 'When a CIM instance array uses single quotes and trailing commas' {
        It 'Should remove the separator lines between the instances' {
            $dscBlock = @(
                "            Members = @(MSFT_TeamMember{"
                "                Name = 'x'"
                "            },"
                "            MSFT_TeamMember{"
                "                Name = 'y'"
                "            });"
                ''
            ) -join "`r`n"

            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Members' -IsCIMArray $true
            $result | Should -Not -Match '\},\r\n'
            $result | Should -Match "Name = 'x'"
        }
    }

    Context 'When the closing parenthesis of a CIM instance array is still quoted' {
        It 'Should move the closing parenthesis onto its own line' {
            $dscBlock = @(
                '            Members              = "@(MSFT_TeamMember{'
                '                DisplayName = `"John Doe'
                '            }");'
                ''
            ) -join "`r`n"

            $expected = @(
                '            Members              = "@(MSFT_TeamMember{'
                '                DisplayName = "John Doe'
                '            }'
                '            );'
                ''
            ) -join "`r`n"

            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Members' -IsCIMArray $true
            $result | Should -Be $expected
        }
    }

    Context 'When converting a CIM object parameter that holds escaped XML' {
        It 'Should remove the surrounding quotes and unescape the attribute quotes' {
            $dscBlock = '            Content = "<Rule Name=`"Block`" Action=`"Deny`" />";' + "`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Content' -IsCIMObject $true
            $result | Should -Be ('            Content = <Rule Name="Block" Action="Deny" />;' + "`r`n")
        }
    }
}

Describe 'Get-DSCBlock' {
    BeforeAll {
        $testModulePath = Join-Path -Path $TestDrive -ChildPath 'TestResource.psm1'
        @'
function Get-TargetResource
{
param(
    [Parameter(Mandatory = $true)]
    [System.String]
    $Name,

    [Parameter()]
    [System.Boolean]
    $Enabled,

    [Parameter()]
    [System.String[]]
    $Items
)
}

function Set-TargetResource
{
param(
    [Parameter(Mandatory = $true)]
    [System.String]
    $Name,

    [Parameter()]
    [System.Boolean]
    $Enabled,

    [Parameter()]
    [System.String[]]
    $Items
)
}
'@ | Set-Content -Path $testModulePath
    }

    Context 'When generating a DSC block for the supported value types' {
        It 'Should quote string values' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Name = 'TestResource' }
            $result | Should -Be "            Name                 = `"TestResource`";`r`n"
        }

        It 'Should format boolean values with a $ prefix' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Enabled = $true }
            $result | Should -Match '= \$True;'
        }

        It 'Should format string arrays with @()' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Items = @('Item1', 'Item2') }
            $result | Should -Match '@\("Item1","Item2"\);'
        }

        It 'Should format an ArrayList as a quoted array' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Items = [System.Collections.ArrayList]@('A', 'B') }
            $result | Should -Match '@\("A","B"\);'
        }

        It 'Should format a generic string list as a quoted array' {
            $list = [System.Collections.Generic.List[System.String]]::new()
            $list.Add('A')
            $list.Add('B')
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Items = $list }
            $result | Should -Match '@\("A","B"\);'
        }

        It 'Should format integer arrays as a bare integer array' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Ports = [System.Int32[]]@(80, 443) }
            $result | Should -Match '@\(80,443\);'
        }

        It 'Should format hashtable values as @{ key = value }' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Items = @{ SubKey = 'SubValue' } }
            $result | Should -Match '@\{SubKey = "SubValue"; \}'
        }

        It 'Should format enum values as a quoted string' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Color = [System.ConsoleColor]::Red }
            $result | Should -Match '= "Red";'
        }

        It 'Should format numeric values without quotes' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Port = 8080 }
            $result | Should -Match '= 8080;'
        }

        It 'Should resolve a credential to its $Creds variable' {
            $securePassword = ConvertTo-SecureString -String 'Password123' -AsPlainText -Force
            $credential = New-Object System.Management.Automation.PSCredential ('CONTOSO\svc-admin', $securePassword)
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Credential = $credential }
            $result | Should -Match '= \$Credssvc_admin;'
        }

        It 'Should format a PSCustomObject the way it always did' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Extra = [PSCustomObject] @{ Z = 1 } }
            $result | Should -Match '= @\{Z=1\};'
        }

    }

    Context 'When aligning the generated parameters' {
        It 'Should pad the parameter names to the default width of 20 characters' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Name = 'Test'; Enabled = $true }
            $result | Should -Match "            Enabled              = "
            $result | Should -Match "            Name                 = "
        }

        It 'Should pad the parameter names to the longest name when it exceeds 20 characters' {
            $params = @{
                Name                                          = 'Test'
                ThisParameterNameIsLongerThanTwentyCharacters = 'Test'
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Match 'ThisParameterNameIsLongerThanTwentyCharacters = "Test";'
            $result | Should -Match 'Name                                          = "Test";'
        }
    }

    Context 'When the parameters are sorted' {
        It 'Should emit the parameters in alphabetical order' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Zeta = 'z'; Alpha = 'a'; Mike = 'm' }
            $result | Should -Match 'Alpha[\s\S]*Mike[\s\S]*Zeta'
        }
    }

    Context 'When _metadata_ properties are present' {
        It 'Should exclude the _metadata_ key and append its value as a comment' {
            $params = @{
                Name           = 'Test'
                _metadata_Name = '# This is a comment'
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Be "            Name                 = `"Test`"; # This is a comment`r`n"
        }
    }

    Context 'When null values are present' {
        It 'Should exclude the parameters with a null value' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Name = 'Test'; Items = $null }
            $result | Should -Not -Match 'Items'
        }
    }

    Context 'When NoEscape is specified for a parameter' {
        It 'Should not escape the specified parameter value' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Name = '$ConfigName' } -NoEscape @('Name')
            $result | Should -Be "            Name                 = `$ConfigName;`r`n"
        }

        It 'Should still escape the parameters that were not listed' {
            $params = @{ Name = '$ConfigName'; Other = '$OtherName' }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params -NoEscape @('Name')
            $result | Should -Match 'Name                 = \$ConfigName;'
            $result | Should -Match 'Other                = "`\$OtherName";'
        }
    }

    Context 'When AllowVariablesInStrings is specified' {
        It 'Should keep the variable references inside the quoted value' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Name = 'Value of $Node' } -AllowVariablesInStrings
            $result | Should -Be "            Name                 = `"Value of `$Node`";`r`n"
        }

        It 'Should keep the variable references inside string arrays' {
            $result = Get-DSCBlock -ModulePath $testModulePath -Params @{ Items = [System.String[]]@('$Node') } -AllowVariablesInStrings
            $result | Should -Match '@\("\$Node"\);'
        }
    }
}

Describe 'Get-DSCParamType' {
    BeforeAll {
        $typeModulePath = Join-Path -Path $TestDrive -ChildPath 'ParamTypeModule.psm1'
        @'
function Set-TargetResource
{
param(
    [System.String] $SystemStringParam,
    [string] $StringParam,
    [System.Boolean] $BooleanParam,
    [boolean] $LowerBooleanParam,
    [bool] $ShortBooleanParam,
    [System.String[]] $StringArrayParam,
    [string[]] $LowerStringArrayParam,
    [Microsoft.Management.Infrastructure.CimInstance] $CimParam,
    [Microsoft.Management.Infrastructure.CimInstance[]] $CimArrayParam,
    [ValidateSet('A', 'B')][System.String] $ValidatedParam
)
}
'@ | Set-Content -Path $typeModulePath
    }

    It 'Should return <Expected> for <ParamName>' -ForEach @(
        @{ ParamName = '$SystemStringParam'; Expected = 'System.String' }
        @{ ParamName = '$StringParam'; Expected = 'System.String' }
        @{ ParamName = '$BooleanParam'; Expected = 'System.Boolean' }
        @{ ParamName = '$LowerBooleanParam'; Expected = 'System.Boolean' }
        @{ ParamName = '$ShortBooleanParam'; Expected = 'System.Boolean' }
        @{ ParamName = '$StringArrayParam'; Expected = 'System.String[]' }
        @{ ParamName = '$LowerStringArrayParam'; Expected = 'System.String[]' }
        @{ ParamName = '$CimParam'; Expected = 'System.Collections.Hashtable' }
        @{ ParamName = '$CimArrayParam'; Expected = 'Microsoft.Management.Infrastructure.CimInstance[]' }
    ) {
        Get-DSCParamType -ModulePath $typeModulePath -ParamName $ParamName | Should -Be $Expected
    }

    It 'Should skip attributes that are not a type and return the parameter type' {
        Get-DSCParamType -ModulePath $typeModulePath -ParamName '$ValidatedParam' | Should -Be 'System.String'
    }

    It 'Should return nothing when the parameter does not exist' {
        Get-DSCParamType -ModulePath $typeModulePath -ParamName '$NonExistent' | Should -BeNullOrEmpty
    }

    It 'Should return nothing when the module has no Set-TargetResource function' {
        $noSetPath = Join-Path -Path $TestDrive -ChildPath 'NoSetTarget.psm1'
        @'
function Get-TargetResource
{
param(
    [System.String] $Name
)
}
'@ | Set-Content -Path $noSetPath
        Get-DSCParamType -ModulePath $noSetPath -ParamName '$Name' | Should -BeNullOrEmpty
    }
}

Describe 'Get-DSCFakeParameters' {
    BeforeAll {
        $fakeModulePath = Join-Path -Path $TestDrive -ChildPath 'FakeParamsModule.psm1'
        @'
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
    [System.String] $Name
)
}
'@ | Set-Content -Path $fakeModulePath

        $fakeParameters = Get-DSCFakeParameters -ModulePath $fakeModulePath
    }

    It 'Should use the first value of a ValidateSet attribute' {
        $fakeParameters['Mode'] | Should -Be 'One'
    }

    It 'Should use the minimum of a ValidateRange attribute' {
        $fakeParameters['Port'] | Should -Be '5'
    }

    It 'Should use an asterisk for string parameters' {
        $fakeParameters['Name'] | Should -Be '*'
    }

    It 'Should use 0 for integer parameters' {
        $fakeParameters['Count'] | Should -Be 0
    }

    It 'Should use null for credential parameters' {
        $fakeParameters.ContainsKey('Credential') | Should -BeTrue
        $fakeParameters['Credential'] | Should -BeNullOrEmpty
    }

    It 'Should use true for boolean parameters' {
        $fakeParameters['Enabled'] | Should -BeTrue
    }

    It 'Should use placeholder values for string array parameters' {
        $fakeParameters['Tags'] | Should -Be '1 2'
    }

    It 'Should not generate a value for CIM instance array parameters' {
        $fakeParameters.ContainsKey('Members') | Should -BeFalse
    }

    It 'Should only look at the Get-TargetResource parameters' {
        $fakeParameters.Keys | Should -HaveCount 7
    }

    It 'Should return an empty hashtable when no Get-TargetResource exists' {
        $noGetPath = Join-Path -Path $TestDrive -ChildPath 'NoGetTarget.psm1'
        @'
function Set-TargetResource
{
param(
    [System.String] $Name
)
}
'@ | Set-Content -Path $noGetPath
        (Get-DSCFakeParameters -ModulePath $noGetPath).Count | Should -Be 0
    }
}

Describe 'Rendering CIM instances' {
    BeforeAll {
        $john = New-TestCimInstance -ClassName 'MSFT_TeamMember' -Properties ([ordered]@{ DisplayName = 'John Doe'; Role = 'Owner' })
        $jane = New-TestCimInstance -ClassName 'MSFT_TeamMember' -Properties ([ordered]@{ DisplayName = 'Jane Roe'; Role = 'Member' })
    }

    Context 'When the instances are a property of a resource' {
        It 'Should render an array of instances inside the DSC block' {
            $expected = @(
                '            DisplayName          = "Contoso Team";'
                '            Members              = @('
                '                MSFT_TeamMember{'
                '                    DisplayName = "John Doe"'
                '                    Role        = "Owner"'
                '                }'
                '                MSFT_TeamMember{'
                '                    DisplayName = "Jane Roe"'
                '                    Role        = "Member"'
                '                }'
                '            );'
                ''
            ) -join "`r`n"

            $params = @{
                DisplayName = 'Contoso Team'
                Members     = [Microsoft.Management.Infrastructure.CimInstance[]]@($john, $jane)
            }
            Get-DSCBlock -ModulePath 'MSFT_ContosoTeam.psm1' -Params $params | Should -Be $expected
        }

        It 'Should render a single instance inside the DSC block' {
            $expected = @(
                '            Owner                = MSFT_TeamMember{'
                '                DisplayName = "John Doe"'
                '                Role        = "Owner"'
                '            };'
                ''
            ) -join "`r`n"

            Get-DSCBlock -ModulePath 'MSFT_ContosoTeam.psm1' -Params @{ Owner = $john } | Should -Be $expected
        }
    }
}

Describe 'Rendering class instances' {
    BeforeAll {
        $john = [MSFT_TestClassMember]::new()
        $john.DisplayName = 'John Doe'
        $john.Role = 'Owner'

        $jane = [MSFT_TestClassMember]::new()
        $jane.DisplayName = 'Jane Roe'
        $jane.Role = 'Member'
    }

    Context 'When the instances are a property of a resource' {
        It 'Should render a single instance inside the DSC block' {
            $expected = @(
                '            Owner                = MSFT_TestClassMember{'
                '                DisplayName = "John Doe"'
                '                Role        = "Owner"'
                '            };'
                ''
            ) -join "`r`n"

            Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Owner = $john } | Should -Be $expected
        }

        It 'Should render a strongly typed array of instances inside the DSC block' {
            $expected = @(
                '            Members              = @('
                '                MSFT_TestClassMember{'
                '                    DisplayName = "John Doe"'
                '                    Role        = "Owner"'
                '                }'
                '                MSFT_TestClassMember{'
                '                    DisplayName = "Jane Roe"'
                '                    Role        = "Member"'
                '                }'
                '            );'
                ''
            ) -join "`r`n"

            Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Members = [MSFT_TestClassMember[]]@($john, $jane) } | Should -Be $expected
        }

        It 'Should render an object array of instances the same way as a strongly typed one' {
            $typed = Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Members = [MSFT_TestClassMember[]]@($john, $jane) }
            $loose = Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Members = @($john, $jane) }

            $loose | Should -Be $typed
        }

        It 'Should render an empty array of instances as an empty array' {
            Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Members = [MSFT_TestClassMember[]]@() } | Should -Be "            Members              = @();`r`n"
        }

        It 'Should not treat an array of a class whose name contains Int as an integer array' {
            $assignment = [MSFT_TestClassIntuneAssignment]::new()
            $assignment.GroupId = '12345'

            $expected = @(
                '            Assignments          = @('
                '                MSFT_TestClassIntuneAssignment{'
                '                    GroupId = "12345"'
                '                }'
                '            );'
                ''
            ) -join "`r`n"

            Get-DSCBlock -ModulePath 'IntuneThing.psm1' -Params @{ Assignments = [MSFT_TestClassIntuneAssignment[]]@($assignment) } | Should -Be $expected
        }

        It 'Should render a graph that nests instances into arrays of instances' {
            $team = [MSFT_TestClassTeam]::new()
            $team.DisplayName = 'Contoso'
            $team.Owner = $john
            $team.Members = [MSFT_TestClassMember[]]@($jane)

            $result = Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Teams = [MSFT_TestClassTeam[]]@($team) }
            $result | Should -Match '                            DisplayName = "Jane Roe"'
            $result | Should -Not -Match 'MSFT_TestClassTeam\s*;'
        }

        It 'Should produce a DSC block that can be parsed' {
            $team = [MSFT_TestClassTeam]::new()
            $team.DisplayName = 'Contoso'
            $team.Members = [MSFT_TestClassMember[]]@($john, $jane)

            $dscBlock = Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ DisplayName = 'Contoso Team' }

            $tokens = $null
            $parseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput("@{`r`n$dscBlock}", [ref] $tokens, [ref] $parseErrors)
            $parseErrors | Should -BeNullOrEmpty
        }

        It 'Should honour NoEscape for the strings of a class instance' {
            $instance = [MSFT_TestClassMember]::new()
            $instance.DisplayName = '$ConfigName'

            $result = Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Owner = $instance } -NoEscape @('Owner')
            $result | Should -Match 'DisplayName = \$ConfigName'
        }

        It 'Should honour AllowVariablesInStrings for the strings of a class instance' {
            $instance = [MSFT_TestClassMember]::new()
            $instance.DisplayName = 'Value of $Node'

            $result = Get-DSCBlock -ModulePath 'ContosoTeam.psm1' -Params @{ Owner = $instance } -AllowVariablesInStrings
            $result | Should -Match 'DisplayName = "Value of \$Node"'
        }
    }
}

Describe 'Extracting a DSC resource instance' {
    BeforeAll {
        $resourcePath = Join-Path -Path $TestDrive -ChildPath 'MSFT_ContosoTeam.psm1'
        @'
function Get-TargetResource
{
    param(
        [Parameter(Mandatory = $true)]
        [System.String]
        $DisplayName,

        [Parameter()]
        [System.String]
        $Description,

        [Parameter()]
        [ValidateSet('Public', 'Private')]
        [System.String]
        $Visibility,

        [Parameter()]
        [System.Boolean]
        $AllowGuests,

        [Parameter()]
        [System.UInt32]
        $MaxMembers,

        [Parameter()]
        [System.String[]]
        $Owners,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $Members,

        [Parameter()]
        [System.Management.Automation.PSCredential]
        $Credential
    )
}

function Set-TargetResource
{
    param(
        [Parameter(Mandatory = $true)]
        [System.String]
        $DisplayName,

        [Parameter()]
        [System.String]
        $Description,

        [Parameter()]
        [ValidateSet('Public', 'Private')]
        [System.String]
        $Visibility,

        [Parameter()]
        [System.Boolean]
        $AllowGuests,

        [Parameter()]
        [System.UInt32]
        $MaxMembers,

        [Parameter()]
        [System.String[]]
        $Owners,

        [Parameter()]
        [Microsoft.Management.Infrastructure.CimInstance[]]
        $Members,

        [Parameter()]
        [System.Management.Automation.PSCredential]
        $Credential
    )
}
'@ | Set-Content -Path $resourcePath
    }

    Context 'When probing the resource for the properties to extract' {
        It 'Should return a fake value for every property the resource exposes' {
            $fakeParameters = Get-DSCFakeParameters -ModulePath $resourcePath

            $fakeParameters['DisplayName'] | Should -Be '*'
            $fakeParameters['Visibility'] | Should -Be 'Public'
            $fakeParameters['AllowGuests'] | Should -BeTrue
            $fakeParameters['MaxMembers'] | Should -Be 0
            $fakeParameters['Owners'] | Should -Be '1 2'
        }

        It 'Should report the declared type of the properties the extract has to convert' {
            Get-DSCParamType -ModulePath $resourcePath -ParamName '$DisplayName' | Should -Be 'System.String'
            Get-DSCParamType -ModulePath $resourcePath -ParamName '$AllowGuests' | Should -Be 'System.Boolean'
            Get-DSCParamType -ModulePath $resourcePath -ParamName '$Owners' | Should -Be 'System.String[]'
            Get-DSCParamType -ModulePath $resourcePath -ParamName '$Members' | Should -Be 'Microsoft.Management.Infrastructure.CimInstance[]'
        }
    }

    Context 'When converting the retrieved values into a DSC block' {
        BeforeAll {
            Save-Credentials -UserName 'CONTOSO\svc-admin'
            Add-ReverseDSCUserName -UserName 'CONTOSO\svc-admin'

            $results = @{
                DisplayName = 'Contoso "Core" Team'
                Description = 'Team that costs $100 per month'
                Visibility  = 'Public'
                AllowGuests = $false
                MaxMembers  = 250
                Owners      = [System.String[]]@('john@contoso.com', 'jane@contoso.com')
                Credential  = Resolve-Credentials -UserName 'CONTOSO\svc-admin'
            }

            $dscBlock = Get-DSCBlock -ModulePath $resourcePath -Params $results -NoEscape @('Credential')
        }

        It 'Should produce a block that PowerShell can parse' {
            $tokens = $null
            $parseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseInput("@{`r`n$dscBlock}", [ref] $tokens, [ref] $parseErrors)
            $parseErrors | Should -BeNullOrEmpty
        }

        It 'Should produce a block that evaluates back to the extracted values' {
            $Credssvc_admin = 'placeholder-credential'
            $evaluated = & ([System.Management.Automation.ScriptBlock]::Create("@{`r`n$dscBlock}"))

            $evaluated.DisplayName | Should -Be 'Contoso "Core" Team'
            $evaluated.Description | Should -Be 'Team that costs $100 per month'
            $evaluated.Visibility | Should -Be 'Public'
            $evaluated.AllowGuests | Should -BeFalse
            $evaluated.MaxMembers | Should -Be 250
            $evaluated.Owners | Should -Be @('john@contoso.com', 'jane@contoso.com')
            $evaluated.Credential | Should -Be 'placeholder-credential'
        }

        It 'Should register the account the extract needs in the destination environment' {
            Test-Credentials -UserName 'CONTOSO\svc-admin' | Should -BeTrue
        }
    }

    Context 'When the resource returns an array of CIM instances' {
        BeforeAll {
            $membersAsString = @(
                '@('
                '                MSFT_TeamMember{'
                '                    DisplayName = "John Doe"'
                '                    Role        = "Owner"'
                '                }'
                '                MSFT_TeamMember{'
                '                    DisplayName = "Jane Roe"'
                '                    Role        = "Member"'
                '                }'
                '            )'
            ) -join "`r`n"

            $results = @{
                DisplayName = 'Contoso Team'
                Members     = $membersAsString
            }

            $dscBlock = Get-DSCBlock -ModulePath $resourcePath -Params $results
            $dscBlock = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Members' -IsCIMArray $true
        }

        It 'Should emit the CIM instances unquoted and unescaped' {
            $expected = @(
                '            DisplayName          = "Contoso Team";'
                '            Members              = @('
                '                MSFT_TeamMember{'
                '                    DisplayName = "John Doe"'
                '                    Role        = "Owner"'
                '                }'
                '                MSFT_TeamMember{'
                '                    DisplayName = "Jane Roe"'
                '                    Role        = "Member"'
                '                }'
                '            );'
                ''
            ) -join "`r`n"

            $dscBlock | Should -Be $expected
        }

        It 'Should leave the other properties escaped and quoted' {
            $dscBlock | Should -Match 'DisplayName          = "Contoso Team";'
        }
    }

    Context 'When the resource returns the CIM instances as objects' {
        BeforeAll {
            $results = @{
                DisplayName = 'Contoso Team'
                Members     = [Microsoft.Management.Infrastructure.CimInstance[]]@(
                    New-TestCimInstance -ClassName 'MSFT_TeamMember' -Properties ([ordered]@{ DisplayName = 'John Doe'; Role = 'Owner' })
                    New-TestCimInstance -ClassName 'MSFT_TeamMember' -Properties ([ordered]@{ DisplayName = 'Jane Roe'; Role = 'Member' })
                )
            }

            $dscBlock = Get-DSCBlock -ModulePath $resourcePath -Params $results
        }

        It 'Should produce the same DSC block as the pre-built string does' {
            $expected = @(
                '            DisplayName          = "Contoso Team";'
                '            Members              = @('
                '                MSFT_TeamMember{'
                '                    DisplayName = "John Doe"'
                '                    Role        = "Owner"'
                '                }'
                '                MSFT_TeamMember{'
                '                    DisplayName = "Jane Roe"'
                '                    Role        = "Member"'
                '                }'
                '            );'
                ''
            ) -join "`r`n"

            $dscBlock | Should -Be $expected
        }

        It 'Should be emitted unescaped so that Convert-DSCStringParamToVariable is not needed' {
            $dscBlock | Should -Not -Match '`"'
        }

        It 'Should lose its property quotes when Convert-DSCStringParamToVariable is applied anyway' {
            $converted = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Members' -IsCIMArray $true
            $converted | Should -Not -Be $dscBlock
            $converted | Should -Match 'DisplayName = John Doe'
        }
    }
}

Describe 'Generating a ConfigurationData document' {
    BeforeAll {
        Clear-ConfigurationDataContent

        Add-ConfigurationDataEntry -Node 'localhost' -Key 'TenantId' -Value 'contoso.onmicrosoft.com' -Description 'The tenant the configuration applies to'
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'ServiceUrls' -Value '@("https://contoso.sharepoint.com","https://contoso-my.sharepoint.com")'
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'Environment' -Value 'Production'
        Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'ApplicationId' -Value '12345678-1234-1234-1234-123456789012'
        Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'Workloads' -Value @('SPO', 'EXO', 'TEAMS') -Description 'Workloads included in the extract'

        $documentPath = Join-Path -Path $TestDrive -ChildPath 'ConfigurationData.psd1'
        New-ConfigurationDataDocument -Path $documentPath

        $document = Get-Content -Path $documentPath -Raw
        $importedData = Import-PowerShellDataFile -Path $documentPath
        $node = $importedData.AllNodes | Where-Object -FilterScript { $_.NodeName -eq 'localhost' }
    }

    It 'Should write a file that PowerShell can import as data' {
        $importedData | Should -Not -BeNullOrEmpty
        $importedData.Keys | Should -Contain 'AllNodes'
        $importedData.Keys | Should -Contain 'NonNodeData'
    }

    It 'Should allow plain text passwords and domain users on the node' {
        $node.PSDscAllowPlainTextPassword | Should -BeTrue
        $node.PSDscAllowDomainUser | Should -BeTrue
    }

    It 'Should round-trip the scalar node entries' {
        $node.TenantId | Should -Be 'contoso.onmicrosoft.com'
        $node.Environment | Should -Be 'Production'
    }

    It 'Should round-trip the array node entries as arrays' {
        $node.ServiceUrls | Should -HaveCount 2
        $node.ServiceUrls | Should -Contain 'https://contoso.sharepoint.com'
    }

    It 'Should round-trip the NonNodeData entries' {
        $importedData.NonNodeData.ApplicationId | Should -Be '12345678-1234-1234-1234-123456789012'
        $importedData.NonNodeData.Workloads | Should -Be @('SPO', 'EXO', 'TEAMS')
    }

    It 'Should document the entries that have a description' {
        $document | Should -Match '# The tenant the configuration applies to'
        $document | Should -Match '# Workloads included in the extract'
    }

    It 'Should not emit an empty comment for the entries without a description' {
        ($document -split "`r`n" | Where-Object -FilterScript { $_.Trim() -eq '#' }) | Should -BeNullOrEmpty
    }
}

Describe 'Module manifest' {
    BeforeAll {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath '..\ReverseDSC\ReverseDSC.psd1'
        $manifest = Test-ModuleManifest -Path $manifestPath -ErrorAction SilentlyContinue
        $manifestData = Import-PowerShellDataFile -Path $manifestPath
    }

    It 'Should be a valid module manifest' {
        $manifest | Should -Not -BeNullOrEmpty
    }

    It 'Should load the compiled assembly as its root module' {
        $manifestData.RootModule | Should -Be 'ReverseDSC.dll'
    }

    It 'Should not export any function' {
        $manifestData.FunctionsToExport | Should -BeNullOrEmpty
    }

    It 'Should only export cmdlets that the assembly defines' {
        $undefined = $manifestData.CmdletsToExport |
            Where-Object -FilterScript { -not (Get-Command -Name $_ -Module 'ReverseDSC' -CommandType Cmdlet -ErrorAction SilentlyContinue) }
        $undefined | Should -BeNullOrEmpty
    }

    It 'Should export <_>' -ForEach @(
        'Clear-ConfigurationDataContent'
        'Clear-ReverseDSCUserNames'
        'Get-DSCParamType'
        'Get-DSCBlock'
        'Get-DSCFakeParameters'
        'Get-DSCDependsOnBlock'
        'Get-Credentials'
        'Resolve-Credentials'
        'Save-Credentials'
        'Test-Credentials'
        'Convert-DSCStringParamToVariable'
        'Get-ConfigurationDataContent'
        'New-ConfigurationDataDocument'
        'Add-ConfigurationDataEntry'
        'Get-ConfigurationDataEntry'
        'Add-ReverseDSCUserName'
    ) {
        $manifestData.CmdletsToExport | Should -Contain $_
    }
}

Describe 'Importing the module' {
    BeforeAll {
        $importedModulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\ReverseDSC\ReverseDSC.psd1'
    }

    It 'Should reset the state that a previous extract accumulated' {
        Save-Credentials -UserName 'CONTOSO\admin'
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'TenantId' -Value 'contoso.onmicrosoft.com'

        Import-Module -Name $importedModulePath -Force

        Test-Credentials -UserName 'CONTOSO\admin' | Should -BeFalse
        Get-ConfigurationDataEntry -Node 'localhost' -Key 'TenantId' | Should -BeNullOrEmpty
    }
}

AfterAll {
    Remove-Module -Name 'ReverseDSC' -ErrorAction SilentlyContinue
}
