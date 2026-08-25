---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Get-Credentials

## SYNOPSIS

Returns the full username (\<domain\>\\\<username\>) of the specified user
if it is already stored in the credentials hashtable.

## SYNTAX

```powershell
Get-Credentials [-UserName] <String> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function checks in the hashtable that stores all the required
credentials (service account, etc.) for the configuration and
returns the fully formatted username.

## PARAMETERS

### -UserName

Name of the user we wish to check to see if it is already stored in our
credentials hashtable.

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

### System.String

## NOTES

## RELATED LINKS
