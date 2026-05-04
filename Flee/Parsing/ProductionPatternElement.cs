using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A production-pattern element. Holds a reference to either a token or a production
    /// along with minimum and maximum occurrence counters that control how many times the
    /// reference may repeat. A production-pattern element is always contained within a
    /// production-pattern alternative.
    /// </summary>
    internal class ProductionPatternElement
    {
        private readonly bool _token;

        /// <summary>
        /// Initializes a new <see cref="ProductionPatternElement"/>, normalizing the
        /// occurrence counters so that <c>0</c> minimums and zero/negative maximums become
        /// <c>0</c> and <see cref="int.MaxValue"/>, respectively.
        /// </summary>
        /// <param name="isToken">Whether the element references a token.</param>
        /// <param name="id">The token or production id.</param>
        /// <param name="min">The minimum occurrence count.</param>
        /// <param name="max">The maximum occurrence count.</param>
        public ProductionPatternElement(bool isToken,
                                        int id,
                                        int min,
                                        int max)
        {

            _token = isToken;
            Id = id;
            if (min < 0)
            {
                min = 0;
            }
            MinCount = min;
            if (max <= 0)
            {
                max = Int32.MaxValue;
            }
            else if (max < min)
            {
                max = min;
            }
            MaxCount = max;
            LookAhead = null!;
        }

        /// <summary>
        /// Gets the token or production id this element references.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Returns the token or production id this element references.
        /// </summary>
        /// <returns>The id.</returns>
        public int GetId()
        {
            return Id;
        }

        /// <summary>
        /// Gets the minimum occurrence count.
        /// </summary>
        public int MinCount { get; }

        /// <summary>
        /// Returns the minimum occurrence count.
        /// </summary>
        /// <returns>The minimum occurrence count.</returns>
        public int GetMinCount()
        {
            return MinCount;
        }

        /// <summary>
        /// Gets the maximum occurrence count.
        /// </summary>
        public int MaxCount { get; }

        /// <summary>
        /// Returns the maximum occurrence count.
        /// </summary>
        /// <returns>The maximum occurrence count.</returns>
        public int GetMaxCount()
        {
            return MaxCount;
        }

        internal LookAheadSet LookAhead { get; set; } = null!;

        /// <summary>
        /// Returns whether this element references a token.
        /// </summary>
        /// <returns><see langword="true"/> when this is a token reference.</returns>
        public bool IsToken()
        {
            return _token;
        }

        /// <summary>
        /// Returns whether this element references a production.
        /// </summary>
        /// <returns><see langword="true"/> when this is a production reference.</returns>
        public bool IsProduction()
        {
            return !_token;
        }

        /// <summary>
        /// Returns whether this element matches <paramref name="token"/>.
        /// </summary>
        /// <param name="token">The candidate token.</param>
        /// <returns><see langword="true"/> when the token matches.</returns>
        public bool IsMatch(Token? token)
        {
            return IsToken() && token != null && token.Id == Id;
        }

        /// <summary>
        /// Returns whether <paramref name="obj"/> is an element with the same kind, id, and
        /// occurrence counters.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><see langword="true"/> when the elements are equal.</returns>
        public override bool Equals(object? obj)
        {
            return obj is ProductionPatternElement elem
                && _token == elem._token
                && Id == elem.Id
                && MinCount == elem.MinCount
                && MaxCount == elem.MaxCount;
        }

        /// <summary>
        /// Returns a hash code consistent with <see cref="Equals(object?)"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return Id * 37;
        }

        /// <summary>
        /// Returns a textual description of this element including its kind and occurrence
        /// counters.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();

            _ = buffer.Append(Id);
            _ = buffer.Append(_token ? "(Token)" : "(Production)");
            if (MinCount != 1 || MaxCount != 1)
            {
                _ = buffer.Append("{");
                _ = buffer.Append(MinCount);
                _ = buffer.Append(",");
                _ = buffer.Append(MaxCount);
                _ = buffer.Append("}");
            }
            return buffer.ToString();
        }
    }
}
