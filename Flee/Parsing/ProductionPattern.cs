using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A production pattern. Represents a set of production alternatives that together form a
    /// single production. A production pattern is uniquely identified by its integer id and
    /// name; the id is used to reference the pattern from
    /// <see cref="ProductionPatternElement"/> instances.
    /// </summary>
    /// <param name="id">The pattern id.</param>
    /// <param name="name">The pattern name.</param>
    internal class ProductionPattern(int id, string name)
    {
        private readonly ArrayList _alternatives = [];
        private int _defaultAlt = -1;

        /// <summary>
        /// Gets the pattern id.
        /// </summary>
        public int Id { get; } = id;

        /// <summary>
        /// Returns the pattern id.
        /// </summary>
        /// <returns>The pattern id.</returns>
        public int GetId()
        {
            return Id;
        }

        /// <summary>
        /// Gets the pattern name.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// Returns the pattern name.
        /// </summary>
        /// <returns>The pattern name.</returns>
        public string GetName()
        {
            return Name;
        }

        /// <summary>
        /// Gets or sets whether this pattern is synthetic (introduced by the parser
        /// generator). Synthetic productions are hidden from analyzers.
        /// </summary>
        public bool Synthetic { get; set; }

        /// <summary>
        /// Returns whether this pattern is synthetic.
        /// </summary>
        /// <returns><see langword="true"/> when synthetic.</returns>
        public bool IsSyntetic()
        {
            return Synthetic;
        }

        /// <summary>
        /// Sets whether this pattern is synthetic.
        /// </summary>
        /// <param name="synthetic">The new value.</param>
        public void SetSyntetic(bool synthetic)
        {
            Synthetic = synthetic;
        }

        internal LookAheadSet LookAhead { get; set; } = null!;

        internal ProductionPatternAlternative? DefaultAlternative
        {
            get
            {
                if (_defaultAlt >= 0)
                {
                    object obj = _alternatives[_defaultAlt]!;
                    return (ProductionPatternAlternative)obj;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                _defaultAlt = 0;
                for (int i = 0; i < _alternatives.Count; i++)
                {
                    if (_alternatives[i] == value)
                    {
                        _defaultAlt = i;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of alternatives in this pattern.
        /// </summary>
        public int Count => _alternatives.Count;

        /// <summary>
        /// Returns the number of alternatives in this pattern.
        /// </summary>
        /// <returns>The alternative count.</returns>
        public int GetAlternativeCount()
        {
            return Count;
        }

        /// <summary>
        /// Gets the alternative at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based alternative index.</param>
        public ProductionPatternAlternative this[int index] => (ProductionPatternAlternative)_alternatives[index]!;

        /// <summary>
        /// Returns the alternative at <paramref name="pos"/>.
        /// </summary>
        /// <param name="pos">The zero-based alternative index.</param>
        /// <returns>The alternative.</returns>
        public ProductionPatternAlternative GetAlternative(int pos)
        {
            return this[pos];
        }

        /// <summary>
        /// Returns whether any alternative in this pattern is left-recursive.
        /// </summary>
        /// <returns><see langword="true"/> when at least one alternative is left-recursive.</returns>
        public bool IsLeftRecursive()
        {
            ProductionPatternAlternative alt;

            for (int i = 0; i < _alternatives.Count; i++)
            {
                alt = (ProductionPatternAlternative)_alternatives[i]!;
                if (alt.IsLeftRecursive())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether any alternative in this pattern is right-recursive.
        /// </summary>
        /// <returns><see langword="true"/> when at least one alternative is right-recursive.</returns>
        public bool IsRightRecursive()
        {
            ProductionPatternAlternative alt;

            for (int i = 0; i < _alternatives.Count; i++)
            {
                alt = (ProductionPatternAlternative)_alternatives[i]!;
                if (alt.IsRightRecursive())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether any alternative in this pattern can match the empty input.
        /// </summary>
        /// <returns><see langword="true"/> when at least one alternative matches empty.</returns>
        public bool IsMatchingEmpty()
        {
            ProductionPatternAlternative alt;

            for (int i = 0; i < _alternatives.Count; i++)
            {
                alt = (ProductionPatternAlternative)_alternatives[i]!;
                if (alt.IsMatchingEmpty())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds <paramref name="alt"/> to the list of alternatives.
        /// </summary>
        /// <param name="alt">The alternative to add.</param>
        /// <exception cref="ParserCreationException">If <paramref name="alt"/> duplicates an existing one.</exception>
        public void AddAlternative(ProductionPatternAlternative alt)
        {
            if (_alternatives.Contains(alt))
            {
                throw new ParserCreationException(
                    ParserCreationException.ErrorType.INVALID_PRODUCTION,
                    Name,
                    "two identical alternatives exist");
            }
            alt.SetPattern(this);
            _ = _alternatives.Add(alt);
        }

        /// <summary>
        /// Returns a textual description of this pattern with its alternatives.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();
            StringBuilder indent = new();
            int i;

            _ = buffer.Append(Name);
            _ = buffer.Append("(");
            _ = buffer.Append(Id);
            _ = buffer.Append(") ");
            for (i = 0; i < buffer.Length; i++)
            {
                _ = indent.Append(" ");
            }
            for (i = 0; i < _alternatives.Count; i++)
            {
                if (i == 0)
                {
                    _ = buffer.Append("= ");
                }
                else
                {
                    _ = buffer.Append("\n");
                    _ = buffer.Append(indent);
                    _ = buffer.Append("| ");
                }
                _ = buffer.Append(_alternatives[i]);
            }
            return buffer.ToString();
        }
    }
}
