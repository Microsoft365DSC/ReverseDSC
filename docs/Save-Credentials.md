---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Save-Credentials

## SYNOPSIS

Adds the specified username to our central list of required credentials.

## SYNTAX

```powershell
Save-Credentials [-UserName] <String> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function checks to see if the specified user is already stored in our
central required credentials list, and if not simply adds it to it.

## PARAMETERS

### -UserName

Username to add to the central list of required credentials.

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
