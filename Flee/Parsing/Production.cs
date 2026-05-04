using System.Collections;

namespace Flee.Parsing
{
    /// <summary>
    /// A production node. Represents a grammar production (a list of child nodes) in a parse
    /// tree. Productions are created by the parser, which adds children according to a set of
    /// production patterns (i.e. grammar rules).
    /// </summary>
    /// <param name="pattern">The pattern that produced this node.</param>
    internal class Production(ProductionPattern pattern) : Node
    {
        private readonly ArrayList _children = [];

        /// <summary>
        /// Gets the production pattern's id.
        /// </summary>
        public override int Id => Pattern.Id;

        /// <summary>
        /// Gets the production pattern's name.
        /// </summary>
        public override string Name => Pattern.Name;

        /// <summary>
        /// Gets the number of direct children.
        /// </summary>
        public override int Count => _children.Count;

        /// <summary>
        /// Gets the child at <paramref name="index"/>, or <see langword="null"/> when the index
        /// is out of range.
        /// </summary>
        /// <param name="index">The zero-based child index.</param>
        public override Node this[int index] => index < 0 || index >= _children.Count ? null! : (Node)_children[index]!;

        /// <summary>
        /// Adds <paramref name="child"/> as a direct child and sets its parent to this node.
        /// </summary>
        /// <param name="child">The child to add. Ignored when <see langword="null"/>.</param>
        public void AddChild(Node child)
        {
            if (child != null)
            {
                child.SetParent(this);
                _ = _children.Add(child);
            }
        }

        /// <summary>
        /// Gets the production pattern that produced this node.
        /// </summary>
        public ProductionPattern Pattern { get; } = pattern;

        /// <summary>
        /// Returns the production pattern that produced this node.
        /// </summary>
        /// <returns>The production pattern.</returns>
        public ProductionPattern GetPattern()
        {
            return Pattern;
        }

        internal override bool IsHidden()
        {
            return Pattern.Synthetic;
        }

        /// <summary>
        /// Returns a short textual description of this node in the form <c>name(id)</c>.
        /// </summary>
        /// <returns>The textual description.</returns>
        public override string ToString()
        {
            return Pattern.Name + '(' + Pattern.Id + ')';
        }
    }
}
