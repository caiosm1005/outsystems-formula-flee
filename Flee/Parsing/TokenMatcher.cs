using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// Common base class for token matchers used by <see cref="Tokenizer"/>. Holds the list
    /// of registered <see cref="TokenPattern"/> instances and exposes the matching contract.
    /// </summary>
    /// <param name="ignoreCase">Whether matching should be case-insensitive.</param>
    internal abstract class TokenMatcher(bool ignoreCase)
    {
        /// <summary>
        /// The patterns registered with this matcher.
        /// </summary>
        protected TokenPattern[] Patterns = [];

        /// <summary>
        /// Whether matching is case-insensitive.
        /// </summary>
        protected bool IgnoreCase = ignoreCase;

        /// <summary>
        /// Runs the matcher against <paramref name="buffer"/>, updating <paramref name="match"/>
        /// when a longer match is found.
        /// </summary>
        /// <param name="buffer">The input buffer.</param>
        /// <param name="match">The match accumulator to update.</param>
        public abstract void Match(ReaderBuffer buffer, TokenMatch match);

        /// <summary>
        /// Returns the registered pattern with id <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The pattern id.</param>
        /// <returns>The matching pattern, or <see langword="null"/> when none is registered.</returns>
        public TokenPattern? GetPattern(int id)
        {
            for (int i = 0; i < Patterns.Length; i++)
            {
                if (Patterns[i].Id == id)
                {
                    return Patterns[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Adds <paramref name="pattern"/> to the matcher. Subclasses override to also
        /// register the pattern with their underlying engine.
        /// </summary>
        /// <param name="pattern">The pattern to add.</param>
        public virtual void AddPattern(TokenPattern pattern)
        {
            Array.Resize(ref Patterns, Patterns.Length + 1);
            Patterns[Patterns.Length - 1] = pattern;
        }

        /// <summary>
        /// Returns a textual description of every registered pattern.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();

            for (int i = 0; i < Patterns.Length; i++)
            {
                _ = buffer.Append(Patterns[i]);
                _ = buffer.Append("\n\n");
            }
            return buffer.ToString();
        }
    }
}
