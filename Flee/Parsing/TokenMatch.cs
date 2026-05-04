namespace Flee.Parsing
{
    /// <summary>
    /// The token-match status. Tracks the longest match found so far across the various
    /// token matchers run by the tokenizer.
    /// </summary>
    internal class TokenMatch
    {
        /// <summary>
        /// Resets the match status, clearing both the length and the matched pattern.
        /// </summary>
        public void Clear()
        {
            Length = 0;
            Pattern = null!;
        }

        /// <summary>
        /// Gets the length of the current best match.
        /// </summary>
        public int Length { get; private set; } = 0;

        /// <summary>
        /// Gets the pattern of the current best match.
        /// </summary>
        public TokenPattern Pattern { get; private set; } = null!;

        /// <summary>
        /// Records <paramref name="pattern"/> as the new best match when its length exceeds
        /// the previous one.
        /// </summary>
        /// <param name="length">The length of the candidate match.</param>
        /// <param name="pattern">The candidate pattern.</param>
        public void Update(int length, TokenPattern pattern)
        {
            if (Length < length)
            {
                Length = length;
                Pattern = pattern;
            }
        }
    }
}
