namespace Flee.Parsing
{
    /// <summary>
    /// <see cref="REHandler"/> implementation that delegates to Grammatica's built-in
    /// <see cref="RegExp"/> engine. Used by the regexp-token matcher when the .NET
    /// <see cref="System.Text.RegularExpressions.Regex"/> engine isn't required.
    /// </summary>
    /// <param name="regex">The regular expression source.</param>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal class GrammaticaRE(string regex, bool ignoreCase) : REHandler
    {
        private readonly RegExp _regExp = new(regex, ignoreCase);
        private Matcher? _matcher = null;

        /// <summary>
        /// Attempts to match the regular expression at the current position of
        /// <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The reader buffer to inspect.</param>
        /// <returns>The match length, or zero when no match was found.</returns>
        public override int Match(ReaderBuffer buffer)
        {
            if (_matcher == null)
            {
                _matcher = _regExp.Matcher(buffer);
            }
            else
            {
                _matcher.Reset(buffer);
            }
            return _matcher.MatchFromBeginning() ? _matcher.Length() : 0;
        }
    }
}
