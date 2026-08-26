---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Clear-ReverseDSCUserNames

## SYNOPSIS

Clears the list of all user accounts required by the source environment.

## SYNTAX

```powershell
Clear-ReverseDSCUserNames [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function clears the list of all user accounts that were
encountered during the extraction process and which are required for
the configuration to work in the destination environment.
This can be
useful to call at the beginning of an extraction to ensure that you are
starting with a clean slate in terms of required user accounts.

## PARAMETERS

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

## NOTES

## RELATED LINKS
