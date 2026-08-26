---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Get-DSCFakeParameters

## SYNOPSIS

Generates a hashtable containing all the properties exposed by the
specified DSC resource but with fake values.

## SYNTAX

```powershell
Get-DSCFakeParameters [-ModulePath] <String> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function scans the specified resource, creates a hashtable with all
the properties it exposes and generates fake values for each property
based on the Data Type assigned to it.

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

### System.Collections.Hashtable

## NOTES

## RELATED LINKS
