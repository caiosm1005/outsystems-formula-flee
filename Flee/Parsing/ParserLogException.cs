using System.Collections;
using System.Text;

namespace Flee.Parsing
{
    /// <summary>
    /// Aggregates one or more <see cref="ParseException"/> instances into a single exception
    /// thrown after parsing finishes. The parser uses this to report every error it
    /// encountered without aborting on the first one.
    /// </summary>
    internal class ParserLogException : Exception
    {
        private readonly ArrayList _errors = [];

        /// <summary>
        /// Initializes a new, empty <see cref="ParserLogException"/>.
        /// </summary>
        public ParserLogException()
        {
        }

        /// <summary>
        /// Gets the combined message containing every aggregated error, separated by newlines.
        /// </summary>
        public override string Message
        {
            get
            {
                StringBuilder buffer = new();

                for (int i = 0; i < Count; i++)
                {
                    if (i > 0)
                    {
                        _ = buffer.Append("\n");
                    }
                    _ = buffer.Append(this[i].Message);
                }
                return buffer.ToString();
            }
        }

        /// <summary>
        /// Gets the number of aggregated errors.
        /// </summary>
        public int Count => _errors.Count;

        /// <summary>
        /// Returns the number of aggregated errors.
        /// </summary>
        /// <returns>The error count.</returns>
        public int GetErrorCount()
        {
            return Count;
        }

        /// <summary>
        /// Returns the aggregated <see cref="ParseException"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The aggregated exception.</returns>
        public ParseException this[int index] => (ParseException)_errors[index]!;

        /// <summary>
        /// Returns the aggregated <see cref="ParseException"/> at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The aggregated exception.</returns>
        public ParseException GetError(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Adds <paramref name="e"/> to the aggregated error list.
        /// </summary>
        /// <param name="e">The exception to aggregate.</param>
        public void AddError(ParseException e)
        {
            _ = _errors.Add(e);
        }

        /// <summary>
        /// Returns the combined message containing every aggregated error.
        /// </summary>
        /// <returns>The combined message.</returns>
        public string GetMessage()
        {
            return Message;
        }
    }
}
