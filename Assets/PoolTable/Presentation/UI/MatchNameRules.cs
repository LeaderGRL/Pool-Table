using System;
using System.Text;

namespace PoolTable.Presentation.UI
{
    public static class MatchNameRules
    {
        public const int MaxLength = 12;

        public static string SanitizeInput(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(Math.Min(value.Length, MaxLength));

            foreach (var character in value)
            {
                if (builder.Length >= MaxLength)
                {
                    break;
                }

                if (char.IsLetterOrDigit(character)
                    || character == ' '
                    || character == '-'
                    || character == '_')
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        public static string NormalizeForMatch(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(fallback))
            {
                throw new ArgumentException("A non-empty fallback player name is required.", nameof(fallback));
            }

            var sanitized = SanitizeInput(value).Trim();
            return sanitized.Length == 0 ? fallback : sanitized;
        }
    }
}
