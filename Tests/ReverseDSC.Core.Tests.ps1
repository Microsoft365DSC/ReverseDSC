# Import module before tests
$modulePath = Join-Path -Path $PSScriptRoot -ChildPath '..\ReverseDSC.Core.psm1'
Import-Module -Name $modulePath -Force

InModuleScope 'ReverseDSC.Core' {
    Describe 'ConvertTo-EscapedDSCString' {
    Context 'When the input string is null or empty' {
        It 'Should return the same empty string' {
            $result = ConvertTo-EscapedDSCString -InputString ''
            $result | Should -BeNullOrEmpty
        }

        It 'Should return null when passed null' {
            $result = ConvertTo-EscapedDSCString -InputString $null
            $result | Should -BeNullOrEmpty
        }
    }

    Context 'When the input string contains backticks' {
        It 'Should escape backticks by doubling them' {
            $result = ConvertTo-EscapedDSCString -InputString 'Hello`World'
            $result | Should -Be 'Hello``World'
        }
    }

    Context 'When the input string contains dollar signs' {
        It 'Should escape dollar signs by default' {
            $result = ConvertTo-EscapedDSCString -InputString 'Price is $100'
            $result | Should -Be 'Price is `$100'
        }

        It 'Should preserve dollar signs when AllowVariables is specified' {
            $result = ConvertTo-EscapedDSCString -InputString 'Value is $var' -AllowVariables
            $result | Should -Be 'Value is $var'
        }
    }

    Context 'When the input string contains European quotation marks' {
        It 'Should escape U+201E (double low-9 quotation mark)' {
            $input201E = "test$([char]0x201E)value"
            $result = ConvertTo-EscapedDSCString -InputString $input201E
            $result | Should -Be "test``$([char]0x201E)value"
        }

        It 'Should escape U+201C (left double quotation mark)' {
            $input201C = "test$([char]0x201C)value"
            $result = ConvertTo-EscapedDSCString -InputString $input201C
            $result | Should -Be "test``$([char]0x201C)value"
        }

        It 'Should escape U+201D (right double quotation mark)' {
            $input201D = "test$([char]0x201D)value"
            $result = ConvertTo-EscapedDSCString -InputString $input201D
            $result | Should -Be "test``$([char]0x201D)value"
        }
    }

    Context 'When the input string contains double quotes' {
        It 'Should escape double quotes' {
            $result = ConvertTo-EscapedDSCString -InputString 'She said "hello"'
            $result | Should -Be 'She said `"hello`"'
        }
    }

    Context 'When the input string contains double quotes and escape characters' {
        It 'Should escape double quotes and escape characters' {
            $result = ConvertTo-EscapedDSCString -InputString 'She said "hello" with `"Escaped Text`"'
            $result | Should -Be 'She said `"hello`" with ```"Escaped Text```"'
        }
    }

    Context 'When the input string is plain text without special characters' {
        It 'Should return the string unchanged' {
            $result = ConvertTo-EscapedDSCString -InputString 'Normal text'
            $result | Should -Be 'Normal text'
        }
    }
}

Describe 'ConvertTo-DSCStringValue' {
    Context 'When the value is null' {
        It 'Should return empty double-quoted string' {
            $result = ConvertTo-DSCStringValue -Value $null
            $result | Should -Be '""'
        }
    }

    Context 'When NoEscape is true' {
        It 'Should return the raw value without escaping' {
            $result = ConvertTo-DSCStringValue -Value 'MyValue' -NoEscape $true
            $result | Should -Be 'MyValue'
        }
    }

    Context 'When NoEscape is false (default)' {
        It 'Should return the value wrapped in double quotes' {
            $result = ConvertTo-DSCStringValue -Value 'SimpleString'
            $result | Should -Be '"SimpleString"'
        }

        It 'Should escape special characters in the value' {
            $result = ConvertTo-DSCStringValue -Value 'Value with $var'
            $result | Should -Be '"Value with `$var"'
        }
    }

    Context 'When AllowVariables is true' {
        It 'Should preserve dollar signs in the value' {
            $result = ConvertTo-DSCStringValue -Value 'Value with $var' -AllowVariables $true
            $result | Should -Be '"Value with $var"'
        }
    }
}

Describe 'ConvertTo-DSCBooleanValue' {
    It 'Should return $True for true values' {
        $result = ConvertTo-DSCBooleanValue -Value $true
        $result | Should -Be '$True'
    }

    It 'Should return $False for false values' {
        $result = ConvertTo-DSCBooleanValue -Value $false
        $result | Should -Be '$False'
    }
}

Describe 'ConvertTo-DSCCredentialValue' {
    Context 'When the value is null' {
        It 'Should return a Get-Credential command with the parameter name' {
            $result = ConvertTo-DSCCredentialValue -Value $null -ParameterName 'Credential'
            $result | Should -Be 'Get-Credential -Message Credential'
        }
    }

    Context 'When the value is a PSCredential with a UPN username' {
        BeforeAll {
            $securePassword = ConvertTo-SecureString -String 'Password123' -AsPlainText -Force
            $credential = New-Object System.Management.Automation.PSCredential ('admin@contoso.com', $securePassword)
        }

        It 'Should return a $Creds variable based on the username part' {
            $result = ConvertTo-DSCCredentialValue -Value $credential -ParameterName 'Credential'
            $result | Should -Be '$Credsadmin'
        }
    }

    Context 'When the value is a PSCredential with a domain\user username' {
        BeforeAll {
            $securePassword = ConvertTo-SecureString -String 'Password123' -AsPlainText -Force
            $credential = New-Object System.Management.Automation.PSCredential ('CONTOSO\admin', $securePassword)
        }

        It 'Should return a $Creds variable based on the username after backslash' {
            $result = ConvertTo-DSCCredentialValue -Value $credential -ParameterName 'Credential'
            $result | Should -Be '$Credsadmin'
        }
    }

    Context 'When the value is a PSCredential with special characters in username' {
        BeforeAll {
            $securePassword = ConvertTo-SecureString -String 'Password123' -AsPlainText -Force
            $credential = New-Object System.Management.Automation.PSCredential ('CONTOSO\admin-user.name', $securePassword)
        }

        It 'Should sanitize special characters in the variable name' {
            $result = ConvertTo-DSCCredentialValue -Value $credential -ParameterName 'Credential'
            $result | Should -Be '$Credsadmin_user_name'
        }
    }
}

Describe 'ConvertTo-DSCHashtableValue' {
    It 'Should format a single-entry hashtable correctly' {
        $hashtable = @{ Key1 = 'Value1' }
        $result = ConvertTo-DSCHashtableValue -Value $hashtable
        $result | Should -BeLike '@{*Key1 = "Value1"*}'
    }

    It 'Should format a multi-entry hashtable correctly' {
        $hashtable = [ordered]@{ Key1 = 'Value1'; Key2 = 'Value2' }
        $result = ConvertTo-DSCHashtableValue -Value $hashtable
        $result | Should -BeLike '@{Key1*Key2*}'
        $result | Should -Match 'Key1 = "Value1"'
        $result | Should -Match 'Key2 = "Value2"'
    }

    It 'Should wrap the result in @{ }' {
        $hashtable = @{ A = 'B' }
        $result = ConvertTo-DSCHashtableValue -Value $hashtable
        $result | Should -Match '^@\{'
        $result | Should -Match '\}$'
    }
}

Describe 'ConvertTo-DSCStringArrayValue' {
    Context 'When the value is null or empty' {
        It 'Should return @() for null value' {
            $result = ConvertTo-DSCStringArrayValue -Value $null
            $result | Should -Be '@()'
        }

        It 'Should return @() for empty array' {
            $result = ConvertTo-DSCStringArrayValue -Value @()
            $result | Should -Be '@()'
        }
    }

    Context 'When the value is a single-element array' {
        It 'Should return a properly formatted array string' {
            $result = ConvertTo-DSCStringArrayValue -Value @('Item1')
            $result | Should -Be '@("Item1")'
        }

        It 'Should return @() for array with null element' {
            $result = ConvertTo-DSCStringArrayValue -Value @( $null )
            $result | Should -Be '@()'
        }
    }

    Context 'When the value is a multi-element array' {
        It 'Should return a comma-separated array string' {
            $result = ConvertTo-DSCStringArrayValue -Value @('Item1', 'Item2', 'Item3')
            $result | Should -Be '@("Item1","Item2","Item3")'
        }
    }

    Context 'When NoEscape is true' {
        It 'Should not escape special characters in array elements' {
            $result = ConvertTo-DSCStringArrayValue -Value @('$var1', '$var2') -NoEscape $true
            $result | Should -Be '@("$var1","$var2")'
        }
    }
}

Describe 'ConvertTo-DSCIntegerArrayValue' {
    Context 'When the value is null or empty' {
        It 'Should return @() for null value' {
            $result = ConvertTo-DSCIntegerArrayValue -Value $null
            $result | Should -Be '@()'
        }

        It 'Should return @() for empty array' {
            $result = ConvertTo-DSCIntegerArrayValue -Value @()
            $result | Should -Be '@()'
        }
    }

    Context 'When the value contains integers' {
        It 'Should return a comma-separated integer array' {
            $result = ConvertTo-DSCIntegerArrayValue -Value @(1, 2, 3)
            $result | Should -Be '@(1,2,3)'
        }

        It 'Should handle a single integer' {
            $result = ConvertTo-DSCIntegerArrayValue -Value @(42)
            $result | Should -Be '@(42)'
        }

        It 'Should return @() for array with null element' {
            $result = ConvertTo-DSCStringArrayValue -Value @( $null )
            $result | Should -Be '@()'
        }
    }
}

Describe 'ConvertTo-DSCObjectArrayValue' {
    Context 'When the value is null or empty' {
        It 'Should return @() for null value' {
            $result = ConvertTo-DSCObjectArrayValue -Value $null
            $result | Should -Be '@()'
        }

        It 'Should return @() for empty array' {
            $result = ConvertTo-DSCObjectArrayValue -Value @()
            $result | Should -Be '@()'
        }
    }

    Context 'When the value contains strings' {
        It 'Should format string elements with quotes' {
            $result = ConvertTo-DSCObjectArrayValue -Value @('A', 'B', 'C')
            $result | Should -Be '@("A","B","C")'
        }

        It 'Should return @() for array with null element' {
            $result = ConvertTo-DSCStringArrayValue -Value @( $null )
            $result | Should -Be '@()'
        }
    }

    Context 'When the value contains hashtables' {
        It 'Should format each hashtable in the array' {
            $value = @(
                @{ Name = 'Item1' }
            )
            $result = ConvertTo-DSCObjectArrayValue -Value $value
            $result | Should -BeLike '@(@{*Name*Item1*})'
        }

        It 'Should handle null values in hashtable entries' {
            $value = @(
                @{ Name = $null }
            )
            $result = ConvertTo-DSCObjectArrayValue -Value $value
            $result | Should -Match '\$null'
        }

        It 'Should handle array values in hashtable entries' {
            $value = @(
                @{ Items = @('A', 'B') }
            )
            $result = ConvertTo-DSCObjectArrayValue -Value $value
            $result | Should -Be "@(@{Items=@('A', 'B')})"
        }
    }

    Context 'When NoEscape is true' {
        It 'Should not escape string values' {
            $result = ConvertTo-DSCObjectArrayValue -Value @('$var') -NoEscape $true
            $result | Should -Match '\$var'
        }
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

Describe 'Save-Credentials' {
    BeforeEach {
        # Reset the credentials repo before each test
        $Script:CredsRepo = @()
    }

    It 'Should add a new username to the credentials repository' {
        Save-Credentials -UserName 'CONTOSO\admin'
        $Script:CredsRepo | Should -Contain 'contoso\admin'
    }

    It 'Should store usernames in lowercase' {
        Save-Credentials -UserName 'CONTOSO\ADMIN'
        $Script:CredsRepo | Should -Contain 'contoso\admin'
    }

    It 'Should not duplicate usernames' {
        Save-Credentials -UserName 'CONTOSO\admin'
        Save-Credentials -UserName 'contoso\admin'
        $Script:CredsRepo | Should -HaveCount 1
    }
}

Describe 'Get-Credentials' {
    BeforeAll {
        $Script:CredsRepo = @()
        Save-Credentials -UserName 'CONTOSO\admin'
    }

    It 'Should return the username when it exists in the repository' {
        $result = Get-Credentials -UserName 'CONTOSO\admin'
        $result | Should -Be 'contoso\admin'
    }

    It 'Should return null when the username is not in the repository' {
        $result = Get-Credentials -UserName 'CONTOSO\nonexistent'
        $result | Should -BeNullOrEmpty
    }
}

Describe 'Test-Credentials' {
    BeforeAll {
        $Script:CredsRepo = @()
        Save-Credentials -UserName 'CONTOSO\admin'
    }

    It 'Should return true when the username exists' {
        $result = Test-Credentials -UserName 'CONTOSO\admin'
        $result | Should -BeTrue
    }

    It 'Should return false when the username does not exist' {
        $result = Test-Credentials -UserName 'CONTOSO\unknown'
        $result | Should -BeFalse
    }
}

Describe 'Resolve-Credentials' {
    It 'Should return $Creds<username> for domain\user format' {
        $result = Resolve-Credentials -UserName 'CONTOSO\admin'
        $result | Should -Be '$Credsadmin'
    }

    It 'Should sanitize hyphens to underscores' {
        $result = Resolve-Credentials -UserName 'CONTOSO\admin-user'
        $result | Should -Be '$Credsadmin_user'
    }

    It 'Should sanitize dots to underscores' {
        $result = Resolve-Credentials -UserName 'CONTOSO\admin.user'
        $result | Should -Be '$Credsadmin_user'
    }

    It 'Should remove spaces and @ signs' {
        $result = Resolve-Credentials -UserName 'admin @company'
        $result | Should -Be '$Credsadmincompany'
    }

    It 'Should handle a simple username without domain' {
        $result = Resolve-Credentials -UserName 'admin'
        $result | Should -Be '$Credsadmin'
    }
}

Describe 'Add-ReverseDSCUserName' {
    BeforeEach {
        $Script:AllUsers = @()
    }

    It 'Should add a username to the list' {
        Add-ReverseDSCUserName -UserName 'user1@contoso.com'
        $Script:AllUsers | Should -Contain 'user1@contoso.com'
    }

    It 'Should not add duplicate usernames' {
        Add-ReverseDSCUserName -UserName 'user1@contoso.com'
        Add-ReverseDSCUserName -UserName 'user1@contoso.com'
        $Script:AllUsers | Should -HaveCount 1
    }
}

Describe 'Get-ReverseDSCUserNames' {
    BeforeAll {
        $Script:AllUsers = @()
        Add-ReverseDSCUserName -UserName 'user1@contoso.com'
        Add-ReverseDSCUserName -UserName 'user2@contoso.com'
    }

    It 'Should return all added usernames' {
        $result = Get-ReverseDSCUserNames
        $result | Should -HaveCount 2
        $result | Should -Contain 'user1@contoso.com'
        $result | Should -Contain 'user2@contoso.com'
    }
}

Describe 'Clear-ReverseDSCUserNames' {
    BeforeAll {
        Add-ReverseDSCUserName -UserName 'user1@contoso.com'
    }

    It 'Should clear all usernames from the list' {
        Clear-ReverseDSCUserNames
        $Script:AllUsers | Should -HaveCount 0
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
    BeforeAll {
        Clear-ConfigurationDataContent
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey' -Value 'TestValue'
    }

    It 'Should return the entry for a specific node and key' {
        $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey'
        $result | Should -Not -BeNullOrEmpty
        $result.Value | Should -Be 'TestValue'
    }

    It 'Should return null when the key does not exist' {
        $result = Get-ConfigurationDataEntry -Node 'localhost' -Key 'NonExistent'
        $result | Should -BeNullOrEmpty
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
    BeforeAll {
        Clear-ConfigurationDataContent
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'ServerName' -Value 'MyServer' -Description 'The server name'
    }

    It 'Should return a string containing the AllNodes section' {
        $result = Get-ConfigurationDataContent
        $result | Should -Match 'AllNodes'
    }

    It 'Should include the node name' {
        $result = Get-ConfigurationDataContent
        $result | Should -Match 'localhost'
    }

    It 'Should include the key and value' {
        $result = Get-ConfigurationDataContent
        $result | Should -Match 'ServerName'
        $result | Should -Match 'MyServer'
    }

    It 'Should include the description as a comment' {
        $result = Get-ConfigurationDataContent
        $result | Should -Match '# The server name'
    }

    It 'Should include NonNodeData section' {
        $result = Get-ConfigurationDataContent
        $result | Should -Match 'NonNodeData'
    }

    It 'Should start with @{ and end with }' {
        $result = Get-ConfigurationDataContent
        $result | Should -Match '^@\{'
        $result | Should -Match '\}$'
    }
}

Describe 'New-ConfigurationDataDocument' {
    BeforeAll {
        Clear-ConfigurationDataContent
        Add-ConfigurationDataEntry -Node 'localhost' -Key 'TestKey' -Value 'TestValue'
        $testPath = Join-Path -Path $TestDrive -ChildPath 'TestConfig.psd1'
    }

    It 'Should create a .psd1 file at the specified path' {
        New-ConfigurationDataDocument -Path $testPath
        Test-Path -Path $testPath | Should -BeTrue
    }

    It 'Should write valid content to the file' {
        New-ConfigurationDataDocument -Path $testPath
        $content = Get-Content -Path $testPath -Raw
        $content | Should -Match 'AllNodes'
        $content | Should -Match 'TestKey'
    }
}

Describe 'ConvertTo-ConfigurationDataString' {
    Context 'When converting a string object' {
        It 'Should wrap the string in quotes with a semicolon' {
            $result = ConvertTo-ConfigurationDataString -PSObject 'TestValue'
            $result | Should -Match '"TestValue"'
        }
    }

    Context 'When converting an array of strings' {
        It 'Should format as a PowerShell array block' {
            $result = ConvertTo-ConfigurationDataString -PSObject @('Item1', 'Item2')
            $result | Should -Match '@\('
            $result | Should -Match 'Item1'
            $result | Should -Match 'Item2'
        }
    }

    Context 'When converting a hashtable' {
        It 'Should format as a PowerShell hashtable block' {
            $hashtable = @{ Name = 'Test' }
            $result = ConvertTo-ConfigurationDataString -PSObject $hashtable
            $result | Should -Match '@\{'
            $result | Should -Match 'Name'
        }
    }
}

Describe 'Convert-DSCStringParamToVariable' {
    Context 'When converting a simple string parameter to a variable' {
        It 'Should remove quotes around the parameter value' {
            $dscBlock = "            ParamName            = `"SomeValue`";`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Not -Match '"SomeValue"'
            $result | Should -Match 'SomeValue'
        }
    }

    Context 'When the parameter name is not found' {
        It 'Should return the original DSCBlock unchanged' {
            $dscBlock = "            OtherParam           = `"Value`";`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'NonExistent'
            $result | Should -Be $dscBlock
        }
    }
}

Describe 'Get-DSCBlock' {
    BeforeAll {
        # Create a minimal DSC resource module for testing
        $testModulePath = Join-Path -Path $TestDrive -ChildPath 'TestResource.psm1'
        $moduleContent = @'
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
'@
        Set-Content -Path $testModulePath -Value $moduleContent
    }

    Context 'When generating a DSC block with string parameters' {
        It 'Should produce a properly formatted DSC configuration block' {
            $params = @{
                Name = 'TestResource'
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Not -BeNullOrEmpty
            $result | Should -Match 'Name'
            $result | Should -Match 'TestResource'
        }
    }

    Context 'When generating a DSC block with boolean parameters' {
        It 'Should format boolean values with $ prefix' {
            $params = @{
                Name    = 'Test'
                Enabled = $true
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Match '= \$True;'
            $result | Should -Match '= "Test";'
        }
    }

    Context 'When generating a DSC block with string array parameters' {
        It 'Should format string arrays with @()' {
            $params = @{
                Name  = 'Test'
                Items = @('Item1', 'Item2')
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Match '@\("Item1","Item2"\);'
            $result | Should -Match '= "Test";'
        }
    }

    Context 'When parameters are aligned' {
        It 'Should pad shorter parameter names with spaces for alignment' {
            $params = @{
                Name    = 'Test'
                Enabled = $true
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            # Both parameters should have equal signs, and shorter names should have more spacing
            $result | Should -Match 'Name\s+='
            $result | Should -Match 'Enabled\s+='
        }
    }

    Context 'When _metadata_ properties are present' {
        It 'Should exclude _metadata_ keys from the output but include their values as comments' {
            $params = @{
                Name              = 'Test'
                _metadata_Name    = '# This is a comment'
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Not -Match '_metadata_'
            $result | Should -Match '# This is a comment'
        }
    }

    Context 'When null values are present' {
        It 'Should exclude parameters with null values' {
            $params = @{
                Name  = 'Test'
                Items = $null
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            # Null params are excluded in the preprocessing step
            $result | Should -Match 'Name'
        }
    }

    Context 'When NoEscape is specified for a parameter' {
        It 'Should not escape the specified parameter values' {
            $params = @{
                Name = '$ConfigName'
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params -NoEscape @('Name')
            $result | Should -Match '\$ConfigName'
            $result | Should -Not -Match '`\$ConfigName'
        }
    }

    Context 'When hashtable parameters are provided' {
        It 'Should format hashtable values as @{ key = value }' {
            $params = @{
                Name  = 'Test'
                Items = @{ SubKey = 'SubValue' }
            }
            $result = Get-DSCBlock -ModulePath $testModulePath -Params $params
            $result | Should -Match '@\{SubKey = "SubValue"; \}'
            $result | Should -Match '= "Test";'
        }
    }
}

Describe 'Module Exports' {
    BeforeAll {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath '..\ReverseDSC.psd1'
        $manifest = Test-ModuleManifest -Path $manifestPath -ErrorAction SilentlyContinue
    }

    It 'Should have a valid module manifest' {
        $manifest | Should -Not -BeNullOrEmpty
    }

    It 'Should export expected functions' -ForEach @(
        @{ FunctionName = 'ConvertTo-EscapedDSCString' }
        @{ FunctionName = 'Get-DSCParamType' }
        @{ FunctionName = 'Get-DSCBlock' }
        @{ FunctionName = 'Get-DSCFakeParameters' }
        @{ FunctionName = 'Get-DSCDependsOnBlock' }
        @{ FunctionName = 'Get-Credentials' }
        @{ FunctionName = 'Resolve-Credentials' }
        @{ FunctionName = 'Save-Credentials' }
        @{ FunctionName = 'Test-Credentials' }
        @{ FunctionName = 'Convert-DSCStringParamToVariable' }
        @{ FunctionName = 'Get-ConfigurationDataContent' }
        @{ FunctionName = 'New-ConfigurationDataDocument' }
        @{ FunctionName = 'Add-ConfigurationDataEntry' }
        @{ FunctionName = 'Get-ConfigurationDataEntry' }
        @{ FunctionName = 'Clear-ConfigurationDataContent' }
        @{ FunctionName = 'Add-ReverseDSCUserName' }
    ) {
        Get-Command -Name $FunctionName -Module 'ReverseDSC.Core' -ErrorAction SilentlyContinue |
            Should -Not -BeNullOrEmpty
    }
}

Describe 'Get-ModuleAst' {
    BeforeAll {
        $astModulePath = Join-Path -Path $TestDrive -ChildPath 'AstTestModule.psm1'
        @'
function Get-TargetResource
{
    param(
        [System.String]
        $Name
    )
}
'@ | Set-Content -Path $astModulePath
    }

    It 'Should parse the module file into a ScriptBlockAst' {
        $ast = Get-ModuleAst -ModulePath $astModulePath
        $ast | Should -BeOfType [System.Management.Automation.Language.ScriptBlockAst]
    }

    It 'Should cache the parsed AST by module path' {
        $ast1 = Get-ModuleAst -ModulePath $astModulePath
        $ast2 = Get-ModuleAst -ModulePath $astModulePath
        $ast1 | Should -Be $ast2
        $Script:ModuleAstCache.ContainsKey($astModulePath) | Should -BeTrue
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
        [System.String[]] $StringArrayParam,
        [string[]] $LowerStringArrayParam,
        [Microsoft.Management.Infrastructure.CimInstance] $CimParam,
        [Microsoft.Management.Infrastructure.CimInstance[]] $CimArrayParam
    )
}
'@ | Set-Content -Path $typeModulePath
    }

    It 'Should return System.String for a System.String parameter' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$SystemStringParam'
        $result | Should -Be 'System.String'
    }

    It 'Should map the short string type to System.String' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$StringParam'
        $result | Should -Be 'System.String'
    }

    It 'Should return System.Boolean for a System.Boolean parameter' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$BooleanParam'
        $result | Should -Be 'System.Boolean'
    }

    It 'Should map the short boolean type to System.Boolean' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$LowerBooleanParam'
        $result | Should -Be 'System.Boolean'
    }

    It 'Should return System.String[] for a System.String[] parameter' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$StringArrayParam'
        $result | Should -Be 'System.String[]'
    }

    It 'Should map the short string[] type to System.String[]' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$LowerStringArrayParam'
        $result | Should -Be 'System.String[]'
    }

    It 'Should map a CimInstance parameter to a Hashtable' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$CimParam'
        $result | Should -Be 'System.Collections.Hashtable'
    }

    It 'Should return the mapped type for a CimInstance[] parameter' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$CimArrayParam'
        $result | Should -Be 'Microsoft.Management.Infrastructure.CimInstance[]'
    }

    It 'Should return nothing when the parameter does not exist' {
        $result = Get-DSCParamType -ModulePath $typeModulePath -ParamName '$NonExistent'
        $result | Should -BeNullOrEmpty
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
        $result = Get-DSCParamType -ModulePath $noSetPath -ParamName '$Name'
        $result | Should -BeNullOrEmpty
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

        [System.String[]] $Tags
    )
}
'@ | Set-Content -Path $fakeModulePath
    }

    It 'Should use the first value of a ValidateSet attribute' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result['Mode'] | Should -Be 'One'
    }

    It 'Should use the minimum of a ValidateRange attribute' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result['Port'] | Should -Be '5'
    }

    It 'Should use an asterisk for string parameters' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result['Name'] | Should -Be '*'
    }

    It 'Should use 0 for integer parameters' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result['Count'] | Should -Be 0
    }

    It 'Should use null for credential parameters' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result.ContainsKey('Credential') | Should -BeTrue
        $result['Credential'] | Should -BeNullOrEmpty
    }

    It 'Should use true for boolean parameters' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result['Enabled'] | Should -BeTrue
    }

    It 'Should use placeholder values for string array parameters' {
        $result = Get-DSCFakeParameters -ModulePath $fakeModulePath
        $result['Tags'] | Should -Be '1 2'
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
        $result = Get-DSCFakeParameters -ModulePath $noGetPath
        $result.Count | Should -Be 0
    }
}

Describe 'Get-DSCBlock - additional value types' {
    BeforeAll {
        $extraModulePath = Join-Path -Path $TestDrive -ChildPath 'ExtraTypesModule.psm1'
        @'
function Get-TargetResource
{
    param(
        [System.String]
        $Name
    )
}
function Set-TargetResource
{
    param(
        [System.String]
        $Name
    )
}
'@ | Set-Content -Path $extraModulePath
    }

    It 'Should format System.String[] values as a quoted array' {
        $params = @{
            Name  = 'Test'
            Items = [string[]]@('A', 'B')
        }
        $result = Get-DSCBlock -ModulePath $extraModulePath -Params $params
        $result | Should -Match '@\("A","B"\);'
    }

    It 'Should format ArrayList values as a quoted array' {
        $params = @{
            Name  = 'Test'
            Items = [System.Collections.ArrayList]@('A', 'B')
        }
        $result = Get-DSCBlock -ModulePath $extraModulePath -Params $params
        $result | Should -Match '@\("A","B"\);'
    }

    It 'Should format integer arrays as a bare integer array' {
        $params = @{
            Name  = 'Test'
            Ports = [int[]]@(80, 443)
        }
        $result = Get-DSCBlock -ModulePath $extraModulePath -Params $params
        $result | Should -Match '@\(80,443\);'
    }

    It 'Should format enum values as a quoted string' {
        $params = @{
            Name  = 'Test'
            Color = [System.ConsoleColor]::Red
        }
        $result = Get-DSCBlock -ModulePath $extraModulePath -Params $params
        $result | Should -Match '= "Red";'
    }

    It 'Should format non-string, non-enum values as a bare value' {
        $params = @{
            Name = 'Test'
            Port = 8080
        }
        $result = Get-DSCBlock -ModulePath $extraModulePath -Params $params
        $result | Should -Match '= 8080;'
    }

    It 'Should handle parameter names longer than 20 characters' {
        $params = @{
            ThisParameterNameIsLongerThanTwentyCharacters = 'Test'
        }
        $result = Get-DSCBlock -ModulePath $extraModulePath -Params $params
        $result | Should -Match 'ThisParameterNameIsLongerThanTwentyCharacters = "Test";'
    }
}

Describe 'Convert-DSCStringParamToVariable - additional scenarios' {
    Context 'When the DSC block has no terminating line breaks' {
        It 'Should remove quotes from the parameter value' {
            $dscBlock = '            ParamName            = "SomeValue"'
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Be '            ParamName            = SomeValue'
        }
    }

    Context 'When converting a CIM array parameter' {
        It 'Should remove quotes from values inside the CIM array' {
            $dscBlock = "            Members = @(`r`n                @{`r`n                    Name = `"x`"`r`n                },`r`n                @{`r`n                    Name = `"y`"`r`n                }`r`n            );`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Members' -IsCIMArray $true
            $result | Should -Match 'Name = x'
            $result | Should -Not -Match 'Name = "x"'
        }
    }

    Context 'When converting a CIM object parameter' {
        It 'Should remove quotes from the escaped XML content' {
            $dscBlock = '            Content = "<xml version=""1.0""><test>""escaped""</test></xml>";' + "`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'Content' -IsCIMObject $true
            $result | Should -Match 'Content = <xml version=1.0><test>escaped</test></xml>;'
        }
    }

    Context 'When the value of another parameter appears after the target parameter' {
        It 'Should only modify the target parameter value' {
            $dscBlock = "            ParamName = `"Value`";`r`n            Other = `"X`";`r`n"
            $result = Convert-DSCStringParamToVariable -DSCBlock $dscBlock -ParameterName 'ParamName'
            $result | Should -Match 'ParamName = Value;'
            $result | Should -Match 'Other = "X";'
        }
    }
}

Describe 'ConvertTo-DSCObjectArrayValue - additional scenarios' {
    It 'Should return @() for an array with a single null element' {
        $result = ConvertTo-DSCObjectArrayValue -Value @( $null )
        $result | Should -Be '@()'
    }

    It 'Should not escape values when NoEscape is specified' {
        $result = ConvertTo-DSCObjectArrayValue -Value @('a', 'b') -NoEscape $true
        $result | Should -Be '@(ab)'
    }

    It 'Should remove a trailing comma in NoEscape mode' {
        $result = ConvertTo-DSCObjectArrayValue -Value @('abc,') -NoEscape $true
        $result | Should -Be '@(abc)'
    }

    It 'Should concatenate non-string, non-hashtable elements' {
        $result = ConvertTo-DSCObjectArrayValue -Value @(1, 2)
        $result | Should -Be '@(12)'
    }
}

Describe 'ConvertTo-DSCIntegerArrayValue - additional scenarios' {
    It 'Should return @() for an array with a single null element' {
        $result = ConvertTo-DSCIntegerArrayValue -Value @( $null )
        $result | Should -Be '@()'
    }
}

Describe 'Get-ConfigurationDataContent - additional scenarios' {
    BeforeEach {
        Clear-ConfigurationDataContent
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

        It 'Should emit a warning when a NonNodeData value cannot be converted' {
            Add-ConfigurationDataEntry -Node 'NonNodeData' -Key 'Bad' -Value 'x'
            $Script:ConfigurationDataContent['NonNodeData'].Entries['Bad'].Value = $null
            { $null = Get-ConfigurationDataContent -WarningAction SilentlyContinue } | Should -Not -Throw
        }
    }
}

Describe 'ConvertTo-ConfigurationDataString - nested structures' {
    It 'Should format an array containing a hashtable' {
        $result = ConvertTo-ConfigurationDataString @(@{ A = 'B' })
        $result | Should -Match '@\('
        $result | Should -Match 'A = "B";'
    }

    It 'Should format a nested array' {
        $result = ConvertTo-ConfigurationDataString @(@('x', 'y'))
        $result | Should -Match '"x";'
        $result | Should -Match '"y";'
    }
}

} # InModuleScope

# Cleanup
Remove-Module -Name 'ReverseDSC.Core' -ErrorAction SilentlyContinue
