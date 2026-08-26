---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Get-ConfigurationDataEntry

## SYNOPSIS

Retrieves the value of a given property in the specified node/section
from the hashtable that is being dynamically built.

## SYNTAX

```powershell
Get-ConfigurationDataEntry [[-Node] <String>] [-Key] <String> [-ProgressAction <ActionPreference>]
 [<CommonParameters>]
```

## DESCRIPTION

This function will return the value of the specified parameter from the
hashtable being dynamically built and which will ultimately become the
content of the ConfigurationData .psd1 file being generated.

## PARAMETERS

### -Node

The name of the node or section in the hashtable we want to look for
the key in.
When omitted, all nodes are searched and the first match
is returned.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key

The name of the parameter to retrieve the value from.

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

### System.Collections.Hashtable

## NOTES

## RELATED LINKS
