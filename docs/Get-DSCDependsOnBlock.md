---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Get-DSCDependsOnBlock

## SYNOPSIS

Generates a string that represents the DependsOn clause based on the
received list of dependencies.

## SYNTAX

```powershell
Get-DSCDependsOnBlock [-DependsOnItems] <Object[]> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function receives an array of strings that represents the list of DSC
resource dependencies for the current DSC block and generates a string
that represents the associated DependsOn DSC string.

## PARAMETERS

### -DependsOnItems

Array of string values that represent the list of dependencies for the
current DSC block.
Objects in the array are expected to be in the form of:
\[\<DSCResourceName\>\]\<InstanceName\>.

```yaml
Type: Object[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProgressAction

{{ Fill ProgressAction Description }}

```yaml
Type: ActionPreference
Parameter Sets: (All)
Aliases: proga

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### System.String

## NOTES

## RELATED LINKS
