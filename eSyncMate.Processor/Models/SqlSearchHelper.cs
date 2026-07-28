namespace eSyncMate.Processor.Models
{
    /// <summary>
    /// Item ids carry characters that mean something to SQL — e.g. 512N-60(3A)[7PC]BKS.
    /// Square brackets, % and _ are LIKE wildcards, so a raw value silently matches the wrong
    /// rows (or none). Values must be escaped before being placed in a criteria string.
    /// </summary>
    public static class SqlSearchHelper
    {
        /// <summary>Doubles single quotes so a value cannot terminate the literal.</summary>
        public static string EscapeLiteral(string p_Value)
        {
            return (p_Value ?? string.Empty).Trim().Replace("'", "''");
        }

        /// <summary>
        /// Escapes a value for use inside LIKE '...'. Wildcards are neutralised with an
        /// explicit escape character, so pair this with <see cref="LikeEscapeClause"/>.
        /// </summary>
        public static string EscapeLike(string p_Value)
        {
            string l_Value = (p_Value ?? string.Empty).Trim();

            // Backslash first — it is the escape character itself
            l_Value = l_Value.Replace("\\", "\\\\")
                             .Replace("%", "\\%")
                             .Replace("_", "\\_")
                             .Replace("[", "\\[")
                             .Replace("]", "\\]");

            return l_Value.Replace("'", "''");
        }

        /// <summary>Appended after a LIKE pattern built with <see cref="EscapeLike"/>.</summary>
        public const string LikeEscapeClause = " ESCAPE '\\'";

        /// <summary>Builds a complete, wildcard-safe "contains" condition.</summary>
        public static string Contains(string p_Column, string p_Value)
        {
            return $" {p_Column} LIKE '%{EscapeLike(p_Value)}%'{LikeEscapeClause}";
        }
    }
}
