---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Test-Credentials

## SYNOPSIS

Checks to see if the specified username is already in our central list of
required credentials.

## SYNTAX

```powershell
Test-Credentials [-UserName] <String> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function checks the central list of required credentials to see if
the specified user is already part of it.
If it finds it, it returns
$true, otherwise it returns $false.

## PARAMETERS

### -UserName

Username to check for existence in the central list of required users.

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

### System.Boolean

## NOTES

## RELATED LINKS
