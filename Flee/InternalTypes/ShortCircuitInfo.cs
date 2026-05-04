using System.Collections;
using System.Reflection.Emit;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Per-emit scratch state for short-circuit boolean evaluation: operand/operator stacks plus
    /// a label cache keyed on the originating expression element.
    /// </summary>
    internal class ShortCircuitInfo
    {
        /// <summary>
        /// Stack of pending operands while building the short-circuit IL.
        /// </summary>
        public Stack Operands;

        /// <summary>
        /// Stack of pending operators while building the short-circuit IL.
        /// </summary>
        public Stack Operators;

        private readonly Dictionary<object, Label> Labels;

        /// <summary>
        /// Initializes empty stacks and an empty label cache.
        /// </summary>
        public ShortCircuitInfo()
        {
            Operands = new Stack();
            Operators = new Stack();
            Labels = [];
        }

        /// <summary>
        /// Clears the operand and operator stacks. The label cache is preserved.
        /// </summary>
        public void ClearTempState()
        {
            Operands.Clear();
            Operators.Clear();
        }

        /// <summary>
        /// Caches <paramref name="lbl"/> under <paramref name="key"/> so siblings of the same
        /// expression can find it during emit.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <param name="lbl">The label to cache.</param>
        /// <returns>The same label, for fluent use.</returns>
        public Label AddLabel(object key, Label lbl)
        {
            Labels.Add(key, lbl);
            return lbl;
        }

        /// <summary>
        /// Returns whether a label has already been cached for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <returns><see langword="true"/> when present.</returns>
        public bool HasLabel(object key)
        {
            return Labels.ContainsKey(key);
        }

        /// <summary>
        /// Returns the label previously cached under <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <returns>The cached label.</returns>
        public Label FindLabel(object key)
        {
            return Labels[key];
        }
    }
}
