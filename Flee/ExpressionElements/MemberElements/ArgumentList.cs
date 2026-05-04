using System.Collections;
using Flee.ExpressionElements.Base;
using Flee.InternalTypes;

namespace Flee.ExpressionElements.MemberElements
{
    /// <summary>
    /// Encapsulates an argument list passed to a function or indexer call. Provides
    /// indexed/typed access for overload resolution and emit.
    /// </summary>
    internal class ArgumentList
    {
        private readonly IList<ExpressionElement> _myElements;

        /// <summary>
        /// Initializes a new list copying the elements from <paramref name="elements"/>.
        /// </summary>
        /// <param name="elements">The argument elements in source order.</param>
        public ArgumentList(ICollection elements)
        {
            ExpressionElement[] arr = new ExpressionElement[elements.Count];
            elements.CopyTo(arr, 0);
            _myElements = arr;
        }

        /// <summary>
        /// Returns the result-type names of the arguments, used in diagnostic messages.
        /// </summary>
        /// <returns>The type names in argument order.</returns>
        private string[] GetArgumentTypeNames()
        {
            List<string> l = [];

            foreach (ExpressionElement e in _myElements)
            {
                l.Add(e.ResultType.Name);
            }

            return [.. l];
        }

        /// <summary>
        /// Returns the result types of the arguments, used by overload resolution.
        /// </summary>
        /// <returns>The types in argument order.</returns>
        public Type[] GetArgumentTypes()
        {
            List<Type> l = [];

            foreach (ExpressionElement e in _myElements)
            {
                l.Add(e.ResultType);
            }

            return [.. l];
        }

        /// <summary>
        /// Returns a comma-separated list of argument type names.
        /// </summary>
        /// <returns>The diagnostic string.</returns>
        public override string ToString()
        {
            string[] typeNames = GetArgumentTypeNames();
            return Utility.FormatList(typeNames);
        }

        /// <summary>
        /// Returns the underlying argument elements as an array.
        /// </summary>
        /// <returns>A copy of the argument elements.</returns>
        public ExpressionElement[] ToArray()
        {
            ExpressionElement[] arr = new ExpressionElement[_myElements.Count];
            _myElements.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>
        /// Gets the argument element at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based argument index.</param>
        /// <returns>The argument element.</returns>
        public ExpressionElement this[int index] => _myElements[index];

        /// <summary>
        /// Gets the argument count.
        /// </summary>
        public int Count => _myElements.Count;
    }
}
