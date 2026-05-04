namespace Flee.Parsing
{
    /// <summary>
    /// <see cref="TokenMatcher"/> implementation backed by an array of regular-expression
    /// handlers. Falls back from Grammatica's regex engine to <see cref="System.Text.RegularExpressions.Regex"/>
    /// for patterns the former cannot compile.
    /// </summary>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal class RegExpMatcher(bool ignoreCase) : TokenMatcher(ignoreCase)
    {
        private REHandler[] _regExps = [];

        /// <summary>
        /// Adds <paramref name="pattern"/> by trying the Grammatica regex engine first and
        /// falling back to the .NET <see cref="System.Text.RegularExpressions.Regex"/> engine on
        /// failure.
        /// </summary>
        /// <param name="pattern">The token pattern to add.</param>
        public override void AddPattern(TokenPattern pattern)
        {
            REHandler re;
            try
            {
                re = new GrammaticaRE(pattern.Pattern, IgnoreCase);
                pattern.DebugInfo = "Grammatica regexp\n" + re;
            }
            catch (Exception)
            {
                re = new SystemRE(pattern.Pattern, IgnoreCase);
                pattern.DebugInfo = "native .NET regexp";
            }
            Array.Resize(ref _regExps, _regExps.Length + 1);
            _regExps[_regExps.Length - 1] = re;
            base.AddPattern(pattern);
        }

        /// <summary>
        /// Runs every registered regex against <paramref name="buffer"/>, updating
        /// <paramref name="match"/> whenever a longer match is found.
        /// </summary>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="match">The match accumulator to update.</param>
        public override void Match(ReaderBuffer buffer, TokenMatch match)
        {
            for (int i = 0; i < _regExps.Length; i++)
            {
                int length = _regExps[i].Match(buffer);
                if (length > 0)
                {
                    match.Update(length, Patterns[i]);
                }
            }
        }
    }
}
