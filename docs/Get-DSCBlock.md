---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Get-DSCBlock

## SYNOPSIS

Generate the DSC string representing the resource's instance.

## SYNTAX

```powershell
Get-DSCBlock [-ModulePath] <String> [-Params] <Hashtable> [[-NoEscape] <String[]>] [-AllowVariablesInStrings]
 [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function is really the core of ReverseDSC.
It takes in an array of
parameters and returns the DSC string that represents the given instance
of the specified resource.

CIM instances, class based complex type instances and arrays of either are
rendered as MOF style blocks that are already unquoted and unescaped.
Only
values that were passed in as a pre-built string still need to be run
through Convert-DSCStringParamToVariable afterwards.

## PARAMETERS

### -ModulePath

Full file path to the .psm1 module we are looking to get an instance of.
In most cases this will be the full path to the .psm1 file of the DSC resource.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Params

Hashtable that contains the list of Key properties and their values.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoEscape

Array of string values that represent the list of parameters that should
not be escaped when generating the DSC string.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AllowVariablesInStrings

When specified, PowerShell variables ($...) inside string values are
preserved instead of being escaped.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
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
