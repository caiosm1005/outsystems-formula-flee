using System.Collections;

namespace Flee.Parsing
{
    /// <summary>
    /// A parse-tree analyzer. Walks a tree of <see cref="Node"/> instances, calling the
    /// <see cref="Enter"/>, <see cref="Child"/>, and <see cref="Exit"/> hooks so subclasses
    /// can transform the tree or compute values for it.
    /// </summary>
    internal class Analyzer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Analyzer"/> class.
        /// </summary>
        public Analyzer()
        {
        }

        /// <summary>
        /// Resets this analyzer when the parser is reset for another input stream. The default
        /// implementation does nothing.
        /// </summary>
        public virtual void Reset()
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Analyzes the parse tree rooted at <paramref name="node"/>, throwing the accumulated
        /// errors if any were reported during traversal.
        /// </summary>
        /// <param name="node">The root node to analyze.</param>
        /// <returns>The transformed root node.</returns>
        /// <exception cref="ParserLogException">If one or more errors were encountered.</exception>
        public Node Analyze(Node node)
        {
            ParserLogException log = new();

            Node? result = Analyze(node, log);
            return log.Count > 0 ? throw log : result!;
        }

        private Node? Analyze(Node node, ParserLogException log)
        {
            var errorCount = log.Count;
            if (node is Production prod)
            {
                prod = NewProduction(prod.Pattern);
                try
                {
                    Enter(prod);
                }
                catch (ParseException e)
                {
                    log.AddError(e);
                }
                for (int i = 0; i < node.Count; i++)
                {
                    try
                    {
                        Child(prod, Analyze(node[i], log)!);
                    }
                    catch (ParseException e)
                    {
                        log.AddError(e);
                    }
                }
                try
                {
                    return Exit(prod);
                }
                catch (ParseException e)
                {
                    if (errorCount == log.Count)
                    {
                        log.AddError(e);
                    }
                }
            }
            else
            {
                node.Values.Clear();
                try
                {
                    Enter(node);
                }
                catch (ParseException e)
                {
                    log.AddError(e);
                }
                try
                {
                    return Exit(node);
                }
                catch (ParseException e)
                {
                    if (errorCount == log.Count)
                    {
                        log.AddError(e);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new production node for <paramref name="pattern"/>. Subclasses can override
        /// this hook to substitute a custom <see cref="Production"/> subclass.
        /// </summary>
        /// <param name="pattern">The production pattern.</param>
        /// <returns>The new production node.</returns>
        public virtual Production NewProduction(ProductionPattern pattern)
        {
            return new Production(pattern);
        }

        /// <summary>
        /// Called when the analyzer enters <paramref name="node"/>. The default implementation
        /// does nothing.
        /// </summary>
        /// <param name="node">The node being entered.</param>
        public virtual void Enter(Node node)
        {
        }

        /// <summary>
        /// Called when the analyzer exits <paramref name="node"/>. Subclasses can override to
        /// replace the node with a different one in the parent's child list.
        /// </summary>
        /// <param name="node">The node being exited.</param>
        /// <returns>The node to attach to the parent (the input node by default).</returns>
        public virtual Node Exit(Node node)
        {
            return node;
        }

        /// <summary>
        /// Called when <paramref name="child"/> has been analyzed and is being attached to
        /// <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The parent production.</param>
        /// <param name="child">The child node to attach.</param>
        public virtual void Child(Production node, Node child)
        {
            node.AddChild(child);
        }

        /// <summary>
        /// Returns the child of <paramref name="node"/> at <paramref name="pos"/>, throwing
        /// when the node or position is invalid.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <param name="pos">The zero-based child index.</param>
        /// <returns>The child node at the requested position.</returns>
        /// <exception cref="ParseException">If the parent or child is missing.</exception>
        protected Node GetChildAt(Node node, int pos)
        {
            if (node == null)
            {
                throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "attempt to read 'null' parse tree node",
                    -1,
                    -1);
            }
            var child = node[pos] ?? throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "node '" + node.Name + "' has no child at " +
                    "position " + pos,
                    node.StartLine,
                    node.StartColumn);
            return child;
        }

        /// <summary>
        /// Returns the first child of <paramref name="node"/> whose <see cref="Node.Id"/>
        /// matches <paramref name="id"/>.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <param name="id">The child id to look up.</param>
        /// <returns>The matching child node.</returns>
        /// <exception cref="ParseException">If no matching child exists.</exception>
        protected Node GetChildWithId(Node node, int id)
        {
            if (node == null)
            {
                throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "attempt to read 'null' parse tree node",
                    -1,
                    -1);
            }
            for (int i = 0; i < node.Count; i++)
            {
                var child = node[i];
                if (child != null && child.Id == id)
                {
                    return child;
                }
            }
            throw new ParseException(
                ParseException.ErrorType.INTERNAL,
                "node '" + node.Name + "' has no child with id " + id,
                node.StartLine,
                node.StartColumn);
        }

        /// <summary>
        /// Returns the value of <paramref name="node"/> at <paramref name="pos"/>.
        /// </summary>
        /// <param name="node">The node carrying the value.</param>
        /// <param name="pos">The zero-based value index.</param>
        /// <returns>The value at the requested position.</returns>
        /// <exception cref="ParseException">If no value is present at the position.</exception>
        protected object GetValue(Node node, int pos)
        {
            if (node == null)
            {
                throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "attempt to read 'null' parse tree node",
                    -1,
                    -1);
            }
            var value = node.Values[pos] ?? throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "node '" + node.Name + "' has no value at " +
                    "position " + pos,
                    node.StartLine,
                    node.StartColumn);
            return value;
        }

        /// <summary>
        /// Returns the value at <paramref name="pos"/> in <paramref name="node"/> as an
        /// <see cref="int"/>.
        /// </summary>
        /// <param name="node">The node carrying the value.</param>
        /// <param name="pos">The zero-based value index.</param>
        /// <returns>The integer value.</returns>
        /// <exception cref="ParseException">If the value is missing or not an integer.</exception>
        protected int GetIntValue(Node node, int pos)
        {
            var value = GetValue(node, pos);
            return value is int
                ? (int)value
                : throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "node '" + node.Name + "' has no integer value " +
                    "at position " + pos,
                    node.StartLine,
                    node.StartColumn);
        }

        /// <summary>
        /// Returns the value at <paramref name="pos"/> in <paramref name="node"/> as a
        /// <see cref="string"/>.
        /// </summary>
        /// <param name="node">The node carrying the value.</param>
        /// <param name="pos">The zero-based value index.</param>
        /// <returns>The string value.</returns>
        /// <exception cref="ParseException">If the value is missing or not a string.</exception>
        protected string GetStringValue(Node node, int pos)
        {
            var value = GetValue(node, pos);
            return value is string
                ? (string)value
                : throw new ParseException(
                    ParseException.ErrorType.INTERNAL,
                    "node '" + node.Name + "' has no string value " +
                    "at position " + pos,
                    node.StartLine,
                    node.StartColumn);
        }

        /// <summary>
        /// Returns a flattened list of every value carried by every child of
        /// <paramref name="node"/>, preserving order.
        /// </summary>
        /// <param name="node">The parent node.</param>
        /// <returns>The combined values from all children.</returns>
        protected ArrayList GetChildValues(Node node)
        {
            ArrayList result = [];

            for (int i = 0; i < node.Count; i++)
            {
                var child = node[i];
                var values = child.Values;
                if (values != null)
                {
                    result.AddRange(values);
                }
            }
            return result;
        }
    }
}
