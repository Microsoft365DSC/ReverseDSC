using System.Text;

namespace ReverseDSC
{
    internal static class DscStringEscaper
    {
        private const char DoubleLowQuotationMark = '„';
        private const char LeftDoubleQuotationMark = '“';
        private const char RightDoubleQuotationMark = '”';

        internal static string Escape(string? inputString, bool allowVariables)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return string.Empty;
            }

            StringBuilder result = new(inputString!.Length + 8);
            foreach (char character in inputString)
            {
                switch (character)
                {
                    case '`':
                        result.Append("``");
                        break;
                    case '$' when !allowVariables:
                        result.Append("`$");
                        break;
                    case DoubleLowQuotationMark:
                    case LeftDoubleQuotationMark:
                    case RightDoubleQuotationMark:
                    case '"':
                        result.Append('`').Append(character);
                        break;
                    default:
                        result.Append(character);
                        break;
                }
            }

            return result.ToString();
        }
    }
}
