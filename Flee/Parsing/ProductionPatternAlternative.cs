using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// A production-pattern alternative. Represents an ordered list of
    /// <see cref="ProductionPatternElement"/> instances. When productions cannot be expressed
    /// with the per-element occurrence counters alone, multiple alternatives must be added
    /// to the same <see cref="ProductionPattern"/>; a production-pattern alternative is
    /// always contained within a production pattern.
    /// </summary>
    internal class ProductionPatternAlternative
    {
        private readonly ArrayList _elements = [];

        /// <summary>
        /// Initializes a new, empty <see cref="ProductionPatternAlternative"/>.
        /// </summary>
        public ProductionPatternAlternative()
        {
        }

        /// <summary>
        /// Gets the production pattern that owns this alternative.
        /// </summary>
        public ProductionPattern Pattern { get; private set; } = null!;

        /// <summary>
        /// Returns the production pattern that owns this alternative.
        /// </summary>
        /// <returns>The owning production pattern.</returns>
        public ProductionPattern GetPattern()
        {
            return Pattern;
        }

        internal LookAheadSet LookAhead { get; set; } = null!;

        /// <summary>
        /// Gets the number of elements in this alternative.
        /// </summary>
        public int Count => _elements.Count;

        /// <summary>
        /// Returns the number of elements in this alternative.
        /// </summary>
        /// <returns>The element count.</returns>
        public int GetElementCount()
        {
            return Count;
        }

        /// <summary>
        /// Gets the element at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based element index.</param>
        public ProductionPatternElement this[int index] => (ProductionPatternElement)_elements[index]!;

        /// <summary>
        /// Returns the element at <paramref name="pos"/>.
        /// </summary>
        /// <param name="pos">The zero-based element index.</param>
        /// <returns>The element.</returns>
        public ProductionPatternElement GetElement(int pos)
        {
            return this[pos];
        }

        /// <summary>
        /// Returns whether this alternative is left-recursive.
        /// </summary>
        /// <returns><see langword="true"/> when the first non-optional element is the owning pattern.</returns>
        public bool IsLeftRecursive()
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                ProductionPatternElement elem = (ProductionPatternElement)_elements[i]!;
                if (elem.Id == Pattern.Id)
                {
                    return true;
                }
                else if (elem.MinCount > 0)
                {
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether this alternative is right-recursive.
        /// </summary>
        /// <returns><see langword="true"/> when the last non-optional element is the owning pattern.</returns>
        public bool IsRightRecursive()
        {
            for (int i = _elements.Count - 1; i >= 0; i--)
            {
                ProductionPatternElement elem = (ProductionPatternElement)_elements[i]!;
                if (elem.Id == Pattern.Id)
                {
                    return true;
                }
                else if (elem.MinCount > 0)
                {
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns whether this alternative can match the empty input.
        /// </summary>
        /// <returns><see langword="true"/> when no non-optional elements are present.</returns>
        public bool IsMatchingEmpty()
        {
            return GetMinElementCount() == 0;
        }

        internal void SetPattern(ProductionPattern pattern)
        {
            Pattern = pattern;
        }

        /// <summary>
        /// Returns the sum of the minimum-count attributes of every element in this
        /// alternative.
        /// </summary>
        /// <returns>The minimum element count.</returns>
        public int GetMinElementCount()
        {
            int min = 0;

            for (int i = 0; i < _elements.Count; i++)
            {
                ProductionPatternElement elem = (ProductionPatternElement)_elements[i]!;
                min += elem.MinCount;
            }
            return min;
        }

        /// <summary>
        /// Returns the sum of the maximum-count attributes of every element in this
        /// alternative, capped at <see cref="int.MaxValue"/>.
        /// </summary>
        /// <returns>The maximum element count.</returns>
        public int GetMaxElementCount()
        {
            int max = 0;

            for (int i = 0; i < _elements.Count; i++)
            {
                ProductionPatternElement elem = (ProductionPatternElement)_elements[i]!;
                if (elem.MaxCount >= Int32.MaxValue)
                {
                    return Int32.MaxValue;
                }
                else
                {
                    max += elem.MaxCount;
                }
            }
            return max;
        }

        /// <summary>
        /// Adds a token-reference element to this alternative.
        /// </summary>
        /// <param name="id">The token id.</param>
        /// <param name="min">The minimum occurrence count.</param>
        /// <param name="max">The maximum occurrence count.</param>
        public void AddToken(int id, int min, int max)
        {
            AddElement(new ProductionPatternElement(true, id, min, max));
        }

        /// <summary>
        /// Adds a production-reference element to this alternative.
        /// </summary>
        /// <param name="id">The production id.</param>
        /// <param name="min">The minimum occurrence count.</param>
        /// <param name="max">The maximum occurrence count.</param>
        public void AddProduction(int id, int min, int max)
        {
            AddElement(new ProductionPatternElement(false, id, min, max));
        }

        /// <summary>
        /// Adds <paramref name="elem"/> to this alternative.
        /// </summary>
        /// <param name="elem">The element to add.</param>
        public void AddElement(ProductionPatternElement elem)
        {
            _ = _elements.Add(elem);
        }

        /// <summary>
        /// Adds a clone of <paramref name="elem"/> with overridden occurrence counters.
        /// </summary>
        /// <param name="elem">The source element.</param>
        /// <param name="min">The minimum occurrence count.</param>
        /// <param name="max">The maximum occurrence count.</param>
        public void AddElement(ProductionPatternElement elem,
                               int min,
                               int max)
        {

            if (elem.IsToken())
            {
                AddToken(elem.Id, min, max);
            }
            else
            {
                AddProduction(elem.Id, min, max);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="obj"/> is an alternative with an element-by-element
        /// equal element list.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><see langword="true"/> when the alternatives are equal.</returns>
        public override bool Equals(object? obj)
        {
            return obj is ProductionPatternAlternative alternative && Equals(alternative);
        }

        /// <summary>
        /// Returns whether <paramref name="alt"/> has an element-by-element equal element list.
        /// </summary>
        /// <param name="alt">The alternative to compare to.</param>
        /// <returns><see langword="true"/> when the alternatives are equal.</returns>
        public bool Equals(ProductionPatternAlternative alt)
        {
            if (_elements.Count != alt._elements.Count)
            {
                return false;
            }
            for (int i = 0; i < _elements.Count; i++)
            {
                if (!_elements[i]!.Equals(alt._elements[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a hash code consistent with <see cref="Equals(object?)"/>.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _elements.Count.GetHashCode();
        }

        /// <summary>
        /// Returns a textual description of this alternative.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            StringBuilder buffer = new();

            for (int i = 0; i < _elements.Count; i++)
            {
                if (i > 0)
                {
                    _ = buffer.Append(" ");
                }
                _ = buffer.Append(_elements[i]);
            }
            return buffer.ToString();
        }
    }
}
