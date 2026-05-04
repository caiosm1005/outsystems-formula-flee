namespace Flee.Parsing
{
    /// <summary>
    /// The dot ('.') character set transition. This transition matches a single character
    /// that is not equal to a newline character.
    /// </summary>
    internal class NFADotTransition(NFAState state) : NFATransition(state)
    {
        /// <summary>
        /// The match set spans the full Unicode range minus newlines, so it isn't ASCII-only.
        /// </summary>
        /// <returns>Always <see langword="false"/>.</returns>
        public override bool IsAscii()
        {
            return false;
        }

        /// <summary>
        /// Returns whether <paramref name="ch"/> is anything other than a Unicode line terminator.
        /// </summary>
        /// <param name="ch">The candidate character.</param>
        /// <returns><see langword="true"/> when <paramref name="ch"/> is not a line terminator.</returns>
        public override bool Match(char ch)
        {
            return ch switch
            {
                '\n' or '\r' or '\u0085' or '\u2028' or '\u2029' => false,
                _ => true,
            };
        }

        /// <summary>
        /// Creates a new dot transition pointing at <paramref name="state"/>.
        /// </summary>
        /// <param name="state">The destination state for the copy.</param>
        /// <returns>The cloned transition.</returns>
        public override NFATransition Copy(NFAState state)
        {
            return new NFADotTransition(state);
        }
    }
}
