using System.Text;

namespace JAXBase.UI.Controls
{
    /// <summary>
    /// Helper class that converts a JAXBase-style mask string into an Avalonia-compatible mask string
    /// and provides metadata needed for the hybrid TextInput handling (positions of ! and ^).
    /// </summary>
    public static class MaskHelper
    {
        /// <summary>
        /// Converts a JAXBase mask to an Avalonia mask and returns metadata for special handling.
        /// </summary>
        /// <param name="jaxMask">The JAXBase mask string (e.g. "!A9#.-/^^^")</param>
        /// <returns>A result containing the Avalonia mask and list of special positions.</returns>
        public static MaskConversionResult ConvertToAvaloniaMask(string jaxMask)
        {
            if (string.IsNullOrEmpty(jaxMask))
            {
                return new MaskConversionResult(string.Empty, new List<SpecialMaskPosition>());
            }

            var avaloniaMask = new StringBuilder();
            var specialPositions = new List<SpecialMaskPosition>();

            for (int i = 0; i < jaxMask.Length; i++)
            {
                char c = jaxMask[i];

                switch (c)
                {
                    case '!':
                        // ! allows any character (mapped to optional any)
                        // Uppercase logic will be handled in TextInput event
                        avaloniaMask.Append('C');
                        specialPositions.Add(new SpecialMaskPosition(i, SpecialMaskType.ForceUpperIfLetter));
                        break;

                    case 'A':
                        // Letters only (optional ASCII letter)
                        avaloniaMask.Append('?');
                        break;

                    case '^':
                        // Uppercase letter only - base mask uses optional letter
                        // Uppercase enforcement will be done in TextInput
                        avaloniaMask.Append('?');
                        specialPositions.Add(new SpecialMaskPosition(i, SpecialMaskType.ForceUpper));
                        break;

                    case '9':
                        // Digits or sign only (optional)
                        avaloniaMask.Append('9');
                        break;

                    case '#':
                        // spaces, digits, or sign (optional)
                        avaloniaMask.Append('#');
                        break;

                    case '.':
                    case '-':
                    case '/':
                        // Literals - pass through directly
                        avaloniaMask.Append(c);
                        break;

                    default:
                        // Unknown character treated as literal (escaped if necessary)
                        if (IsMaskSpecialChar(c))
                        {
                            avaloniaMask.Append('\\');
                        }
                        avaloniaMask.Append(c);
                        break;
                }
            }

            return new MaskConversionResult(avaloniaMask.ToString(), specialPositions);
        }

        private static bool IsMaskSpecialChar(char c)
        {
            // Avalonia mask special characters that need escaping when used as literals
            return c is '0' or '9' or '#' or 'L' or '?' or '&' or 'C' or '>' or '<' or '.' or ',' or ':' or '/' or '-' or '\\';
        }
    }

    /// <summary>
    /// Result of mask conversion.
    /// </summary>
    public class MaskConversionResult
    {
        public string AvaloniaMask { get; }
        public IReadOnlyList<SpecialMaskPosition> SpecialPositions { get; }

        public MaskConversionResult(string avaloniaMask, List<SpecialMaskPosition> specialPositions)
        {
            AvaloniaMask = avaloniaMask ?? string.Empty;
            SpecialPositions = specialPositions ?? new List<SpecialMaskPosition>();
        }
    }

    /// <summary>
    /// Represents a position that needs special handling in TextInput.
    /// </summary>
    public class SpecialMaskPosition
    {
        public int Position { get; }
        public SpecialMaskType Type { get; }

        public SpecialMaskPosition(int position, SpecialMaskType type)
        {
            Position = position;
            Type = type;
        }
    }

    /// <summary>
    /// Type of special handling required for a mask position.
    /// </summary>
    public enum SpecialMaskType
    {
        ForceUpperIfLetter,   // for '!'
        ForceUpper            // for '^'
    }
}