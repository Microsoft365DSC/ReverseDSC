---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Resolve-Credentials

## SYNOPSIS

Returns a string representing the name of the PSCredential variable
associated with the specified username.

## SYNTAX

```powershell
Resolve-Credentials [-UserName] <String> [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function takes in a specified user name and returns what the
standardized variable name for that user should be inside of our
extracted DSC configuration.
Credential variables will always be named
$Creds\<username\> as a standard for ReverseDSC.
This function makes sure
that the variable name does not contain characters that are invalid in
variable names but might be valid in usernames.

## PARAMETERS

### -UserName

Name of the user we wish to get the associated variable name from.

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
