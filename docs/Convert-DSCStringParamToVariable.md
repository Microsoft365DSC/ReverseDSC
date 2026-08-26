---
external help file: ReverseDSC.dll-Help.xml
Module Name: ReverseDSC
online version:
schema: 2.0.0
---

# Convert-DSCStringParamToVariable

## SYNOPSIS

Removes quotes around a parameter in the resulting DSC config,
effectively converting it to a variable instead of a string value.

## SYNTAX

```powershell
Convert-DSCStringParamToVariable [-DSCBlock] <String> [-ParameterName] <String> [[-IsCIMArray] <Boolean>]
 [[-IsCIMObject] <Boolean>] [-ProgressAction <ActionPreference>] [<CommonParameters>]
```

## DESCRIPTION

This function will scan the content of the current DSC block for the
resource, find the specified parameter and remove quotes around its
value so that it becomes a variable instead of a string value.

## PARAMETERS

### -DSCBlock

The string representation of the current DSC resource instance we
are extracting along with all of its parameters and values.

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

### -ParameterName

The name of the parameter we wish to convert the value as a variable
instead of a string value for.

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

### -IsCIMArray

Represents whether or not the parameter to convert to a variable is an
array of CIM instances or not.
We need to differentiate by explicitly
passing in this parameter because to the function a CIMArray is nothing
but a System.Object\[\] and will treat it as such.
CIMArrays differ in
that we should not have commas in between items they contain.

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsCIMObject

Represents whether or not the parameter to convert to a variable is a
CIM instance or not.
We need to differentiate by explicitly passing
in this parameter because to the function a CIMObject is nothing
but a String object and will treat it as such.
However it has escaped
double quotes, which need to be handled properly.

```yaml
Type: Boolean
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
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

Class based complex types are rendered as unquoted and unescaped blocks as
well and must likewise not be passed through this function.

## RELATED LINKS
