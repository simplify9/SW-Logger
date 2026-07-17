using System.Collections.Generic;
using System.Linq;

namespace SW.Logger.ElasticSerach
{
    /// <summary>
    /// Internal string helper utilities for the SW ElasticSearch Logger.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Validates that <paramref name="name"/> is a legal ElasticSearch index name.
        /// <para>
        /// Rules enforced:
        /// <list type="bullet">
        ///   <item><description>Must be all lowercase.</description></item>
        ///   <item><description>Must not contain the characters <c>/ \ * ? " &lt; &gt; ' | :</c>.</description></item>
        ///   <item><description>Must not start with <c>-</c>, <c>_</c>, or <c>+</c>.</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="name">The candidate index name to validate.</param>
        /// <returns>
        /// A tuple of <c>(true, null)</c> when the name is valid, or
        /// <c>(false, errorMessage)</c> listing all validation failures.
        /// </returns>
        public static (bool, string) IsValidIndexName(this string name)
        {
            var errorMessages = new List<string>();
            var invalidChars = new List<char> {'/', '\\', '*', '?', '"', '<', '>', '\'', '|',':'};
            if (name.Any(char.IsUpper))
                errorMessages.Add("Elastic search cannot contain upper case letters");
            foreach (var notAllowedChar in invalidChars)
            {
                if (name.Contains(notAllowedChar))
                    errorMessages.Add($"Name can't contain: '{notAllowedChar}' character(s)");
            }

            var invalidStartChars = new List<char> {'-', '_', '+'};

            foreach (var invalidStartChar in invalidStartChars)
            {
                if (name.StartsWith(invalidStartChar))
                    errorMessages.Add($"Name can't start with '{invalidStartChar}' character(s)");
            }

            if (!errorMessages.Any()) return (true, null);
            var message = string.Join(", ", errorMessages);
            return (false,
                $"Invalid ApplicationName '{name}' in SwLogger environment settings with validation errors: {message}");

        }
    }
}