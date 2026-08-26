---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Add-ReverseDSCUserName

## SYNOPSIS

Adds the provided username to the list of required users for the
destination environment.

## SYNTAX

```powershell
Add-ReverseDSCUserName [-UserName] <String> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

ReverseDSC allows you to keep track of all user credentials encountered
during various stages of the extraction process.
By keeping a central
list of all user accounts required by the source environment we can
easily generate a script that will automatically create new user
placeholders in a destination environment's Active Directory.
This
function checks to see if the specified user was already encountered,
and if not adds it to the central list of all required users.

## PARAMETERS

### -UserName

Name of the user to add to the central list of required users.

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

## NOTES

## RELATED LINKS
