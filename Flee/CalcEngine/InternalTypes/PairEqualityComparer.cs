namespace Flee.CalcEngine.InternalTypes
{
    /// <summary>
    /// Equality comparer for <see cref="ExpressionResultPair"/> that matches on name using
    /// ordinal-case-insensitive comparison.
    /// </summary>
    internal class PairEqualityComparer : EqualityComparer<ExpressionResultPair>
    {
        /// <summary>
        /// Returns whether <paramref name="x"/> and <paramref name="y"/> have the same name.
        /// </summary>
        /// <param name="x">The first pair.</param>
        /// <param name="y">The second pair.</param>
        /// <returns><see langword="true"/> when the names match.</returns>
        public override bool Equals(ExpressionResultPair? x, ExpressionResultPair? y)
        {
            return string.Equals(x?.Name, y?.Name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns an ordinal-case-insensitive hash of <paramref name="obj"/>'s name.
        /// </summary>
        /// <param name="obj">The pair to hash.</param>
        /// <returns>The hash code.</returns>
        public override int GetHashCode(ExpressionResultPair obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
        }
    }
}
