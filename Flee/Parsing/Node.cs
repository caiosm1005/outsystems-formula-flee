using System.Collections;

namespace Flee.Parsing
{
    /// <summary>
    /// Common base class for parse-tree nodes. Inherited by both <see cref="Token"/> and
    /// <see cref="Production"/>, exposing the structural and positional information shared by
    /// every node in the tree.
    /// </summary>
    internal abstract class Node
    {
        private ArrayList? _values;

        internal virtual bool IsHidden()
        {
            return false;
        }

        /// <summary>
        /// Gets the unique identifier of this node (typically the token or production id).
        /// </summary>
        public abstract int Id
        {
            get;
        }

        /// <summary>
        /// Returns the unique identifier of this node.
        /// </summary>
        /// <returns>The node id.</returns>
        public virtual int GetId()
        {
            return Id;
        }

        /// <summary>
        /// Gets the human-readable name of this node.
        /// </summary>
        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// Returns the human-readable name of this node.
        /// </summary>
        /// <returns>The node name.</returns>
        public virtual string GetName()
        {
            return Name;
        }

        /// <summary>
        /// Gets the line where this node begins in the source, or <c>-1</c> when unknown.
        /// </summary>
        public virtual int StartLine
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    var line = this[i].StartLine;
                    if (line >= 0)
                    {
                        return line;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        /// Returns the line where this node begins in the source.
        /// </summary>
        /// <returns>The starting line, or <c>-1</c> when unknown.</returns>
        public virtual int GetStartLine()
        {
            return StartLine;
        }

        /// <summary>
        /// Gets the column where this node begins in the source, or <c>-1</c> when unknown.
        /// </summary>
        public virtual int StartColumn
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    var col = this[i].StartColumn;
                    if (col >= 0)
                    {
                        return col;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        /// Returns the column where this node begins in the source.
        /// </summary>
        /// <returns>The starting column, or <c>-1</c> when unknown.</returns>
        public virtual int GetStartColumn()
        {
            return StartColumn;
        }

        /// <summary>
        /// Gets the line where this node ends in the source, or <c>-1</c> when unknown.
        /// </summary>
        public virtual int EndLine
        {
            get
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    var line = this[i].EndLine;
                    if (line >= 0)
                    {
                        return line;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        /// Returns the line where this node ends in the source.
        /// </summary>
        /// <returns>The ending line, or <c>-1</c> when unknown.</returns>
        public virtual int GetEndLine()
        {
            return EndLine;
        }

        /// <summary>
        /// Gets the column where this node ends in the source, or <c>-1</c> when unknown.
        /// </summary>
        public virtual int EndColumn
        {
            get
            {
                int col;

                for (int i = Count - 1; i >= 0; i--)
                {
                    col = this[i].EndColumn;
                    if (col >= 0)
                    {
                        return col;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        /// Returns the column where this node ends in the source.
        /// </summary>
        /// <returns>The ending column, or <c>-1</c> when unknown.</returns>
        public virtual int GetEndColumn()
        {
            return EndColumn;
        }

        /// <summary>
        /// Gets the parent node, or <see langword="null"/> when this node is the root.
        /// </summary>
        public Node? Parent { get; private set; }

        /// <summary>
        /// Returns the parent node.
        /// </summary>
        /// <returns>The parent node, or <see langword="null"/> when this node is the root.</returns>
        public Node? GetParent()
        {
            return Parent;
        }

        internal void SetParent(Node parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// Gets the number of direct children of this node.
        /// </summary>
        public virtual int Count => 0;

        /// <summary>
        /// Returns the number of direct children of this node.
        /// </summary>
        /// <returns>The child count.</returns>
        public virtual int GetChildCount()
        {
            return Count;
        }

        /// <summary>
        /// Returns the total number of descendants of this node.
        /// </summary>
        /// <returns>The descendant count.</returns>
        public int GetDescendantCount()
        {
            int count = 0;

            for (int i = 0; i < Count; i++)
            {
                count += 1 + this[i].GetDescendantCount();
            }
            return count;
        }

        /// <summary>
        /// Gets the child at <paramref name="index"/>, or <see langword="null"/> when none.
        /// </summary>
        /// <param name="index">The zero-based child index.</param>
        public virtual Node this[int index] => null!;

        /// <summary>
        /// Returns the child at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based child index.</param>
        /// <returns>The child node, or <see langword="null"/> when none.</returns>
        public virtual Node? GetChildAt(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Gets the values associated with this node, lazily allocating the backing list.
        /// </summary>
        public ArrayList Values
        {
            get
            {
                _values ??= [];
                return _values;
            }

            set => _values = value;
        }

        /// <summary>
        /// Returns the number of values associated with this node.
        /// </summary>
        /// <returns>The value count.</returns>
        public int GetValueCount()
        {
            return _values == null ? 0 : _values.Count;
        }

        /// <summary>
        /// Returns the value at <paramref name="pos"/>.
        /// </summary>
        /// <param name="pos">The zero-based value index.</param>
        /// <returns>The value at the requested position.</returns>
        public object GetValue(int pos)
        {
            return Values[pos]!;
        }

        /// <summary>
        /// Returns the underlying value list, or <see langword="null"/> when none has been
        /// allocated.
        /// </summary>
        /// <returns>The value list, or <see langword="null"/>.</returns>
        public ArrayList? GetAllValues()
        {
            return _values;
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the value list, ignoring <see langword="null"/>.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void AddValue(object value)
        {
            if (value != null)
            {
                _ = Values.Add(value);
            }
        }

        /// <summary>
        /// Adds every entry in <paramref name="values"/> to the value list.
        /// </summary>
        /// <param name="values">The values to add.</param>
        public void AddValues(ArrayList values)
        {
            if (values != null)
            {
                Values.AddRange(values);
            }
        }

        /// <summary>
        /// Removes every value associated with this node.
        /// </summary>
        public void RemoveAllValues()
        {
            _values = null;
        }

        /// <summary>
        /// Writes a textual representation of the parse tree rooted at this node to
        /// <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The text writer that receives the description.</param>
        public void PrintTo(TextWriter output)
        {
            PrintTo(output, "");
            output.Flush();
        }

        private void PrintTo(TextWriter output, string indent)
        {
            output.WriteLine(indent + ToString());
            indent += "  ";
            for (int i = 0; i < Count; i++)
            {
                this[i].PrintTo(output, indent);
            }
        }
    }
}
