using System;
using System.Text.RegularExpressions;

namespace ReverseDSC
{
    internal static class DscStringParamConverter
    {
        private const string EndOfLine = ";\r\n";
        private const string NestedPropertyWithSpace = "= `\"";
        private const string NestedPropertyWithoutSpace = "=`\"";
        private const string QuotedClosingParenthesis = "}\");";
        private const string UnquotedClosingParenthesis = "}\r\n            );";
        private const string EscapedDoubleQuote = "`\"";

        private static readonly Regex SeparatorLine =
            new("\r\n\\s*[,;]\r\n", RegexOptions.CultureInvariant);

        internal static string Convert(string dscBlock, string parameterName, bool isCimArray, bool isCimObject)
        {
            bool skipNestedQuotes = isCimArray || isCimObject;
            int startPosition = FindParameterAssignment(dscBlock, parameterName);
            int endOfLinePosition = 0;

            if (startPosition != -1)
            {
                endOfLinePosition = EndOfLineFrom(dscBlock, startPosition);
                startPosition = dscBlock.IndexOf("\"", startPosition, StringComparison.Ordinal);
            }

            while (startPosition >= 0 && startPosition < endOfLinePosition)
            {
                endOfLinePosition = EndOfLineFrom(dscBlock, startPosition);
                if (endOfLinePosition > startPosition)
                {
                    int endPosition = dscBlock.IndexOf("\"", startPosition + 1, StringComparison.Ordinal);
                    if (skipNestedQuotes)
                    {
                        endPosition = SkipQuotesBelongingToNestedProperties(dscBlock, endPosition);
                    }

                    if (endPosition < 0)
                    {
                        endPosition = dscBlock.IndexOf("'", startPosition + 1, StringComparison.Ordinal);
                    }

                    if (endPosition >= 0 && endPosition <= endOfLinePosition)
                    {
                        dscBlock = dscBlock.Remove(startPosition, 1);
                        dscBlock = dscBlock.Remove(endPosition - 1, 1);
                    }
                    else
                    {
                        startPosition = -1;
                    }
                }

                if (startPosition < 0)
                {
                    break;
                }

                startPosition = dscBlock.IndexOf("\"", startPosition, StringComparison.Ordinal);
                if (skipNestedQuotes)
                {
                    startPosition = SkipQuotesBelongingToNestedProperties(dscBlock, startPosition);
                }
            }

            if (!skipNestedQuotes)
            {
                return dscBlock;
            }

            dscBlock = dscBlock.Replace("},\r\n", "}\r\n");
            dscBlock = SeparatorLine.Replace(dscBlock, "\r\n");
            dscBlock = MoveTrailingParenthesisToOwnLine(dscBlock, parameterName);
            return UnescapeDoubleQuotes(dscBlock, parameterName);
        }

        private static int FindParameterAssignment(string dscBlock, string parameterName)
        {
            string marker = " " + parameterName + " ";
            int startPosition = -1;
            int assignmentPosition = -1;
            int quotePosition = -1;

            do
            {
                startPosition = dscBlock.IndexOf(marker, startPosition + 1, StringComparison.Ordinal);
                if (startPosition != -1)
                {
                    assignmentPosition = dscBlock.IndexOf("=", startPosition, StringComparison.Ordinal);
                    quotePosition = dscBlock.IndexOf("\"", startPosition, StringComparison.Ordinal);
                }
            }
            while (assignmentPosition > quotePosition && startPosition != -1);

            return startPosition;
        }

        private static int EndOfLineFrom(string dscBlock, int position)
        {
            int endOfLinePosition = dscBlock.IndexOf(EndOfLine, position, StringComparison.Ordinal);
            return endOfLinePosition == -1 ? dscBlock.Length : endOfLinePosition;
        }

        private static int SkipQuotesBelongingToNestedProperties(string dscBlock, int position)
        {
            while (position > 1 && IsNestedPropertyQuote(dscBlock, position))
            {
                position = dscBlock.IndexOf("\"", position + 1, StringComparison.Ordinal);
                position = dscBlock.IndexOf("\"", position + 1, StringComparison.Ordinal);
            }

            return position;
        }

        private static bool IsNestedPropertyQuote(string dscBlock, int position)
        {
            return string.Equals(dscBlock.Substring(position - 3, 4), NestedPropertyWithSpace, StringComparison.Ordinal)
                || string.Equals(dscBlock.Substring(position - 2, 3), NestedPropertyWithoutSpace, StringComparison.Ordinal);
        }

        private static string MoveTrailingParenthesisToOwnLine(string dscBlock, string parameterName)
        {
            string? propertyString = GetPropertyString(dscBlock, parameterName);
            if (propertyString is null || !propertyString.EndsWith(QuotedClosingParenthesis, StringComparison.Ordinal))
            {
                return dscBlock;
            }

            return dscBlock.Replace(propertyString, propertyString.Replace(QuotedClosingParenthesis, UnquotedClosingParenthesis));
        }

        private static string UnescapeDoubleQuotes(string dscBlock, string parameterName)
        {
            string? propertyString = GetPropertyString(dscBlock, parameterName);
            if (propertyString is null || propertyString.IndexOf(EscapedDoubleQuote, StringComparison.Ordinal) < 0)
            {
                return dscBlock;
            }

            return dscBlock.Replace(propertyString, propertyString.Replace(EscapedDoubleQuote, "\""));
        }

        private static string? GetPropertyString(string dscBlock, string parameterName)
        {
            int indexOfProperty = dscBlock.IndexOf(parameterName, StringComparison.Ordinal);
            if (indexOfProperty < 0)
            {
                return null;
            }

            int indexOfEndOfLine = dscBlock.IndexOf(EndOfLine, indexOfProperty, StringComparison.Ordinal);
            if (indexOfEndOfLine <= 0 || indexOfEndOfLine <= indexOfProperty)
            {
                return null;
            }

            return dscBlock.Substring(indexOfProperty, indexOfEndOfLine - indexOfProperty + 1);
        }
    }
}
