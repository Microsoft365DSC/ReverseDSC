---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Get-DSCParamType

## SYNOPSIS

Retrieves the data type of a specific parameter from the associated DSC resource.

## SYNTAX

```powershell
Get-DSCParamType [-ModulePath] <String> [-ParamName] <String> [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION

This function scans the specified module (or in this case DSC resource),
checks for the specified parameter inside the .schema.mof file associated
with that module and properly assesses and returns the Data Type assigned
to the parameter.

## PARAMETERS

### -ModulePath

Full file path to the .psm1 module we are looking for the property inside of.
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

### -ParamName

Name of the parameter in the module we want to determine the Data Type for.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 2
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
