using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Literals;
using Flee.InternalTypes;
using Flee.PublicTypes;
using Flee.Resources;

namespace Flee.ExpressionElements
{
    /// <summary>
    /// Regex-match operator (<c>x MATCH /pattern/flags</c>). The right-hand side must be a
    /// REGEXP literal (the contextual lexer guarantees this), so the regex is compiled
    /// once via <see cref="RegexCache"/> at first evaluation.
    /// </summary>
    internal class MatchElement : ExpressionElement
    {
        private static readonly MethodInfo _isMatchMethod =
            typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string)])!;

        private readonly ExpressionElement _input;
        private readonly RegexLiteralElement _regex;

        /// <summary>
        /// Initializes a new instance and validates that the input operand resolves to a string.
        /// </summary>
        /// <param name="input">The left-hand string to test.</param>
        /// <param name="regex">The right-hand regex literal.</param>
        public MatchElement(ExpressionElement input, RegexLiteralElement regex)
        {
            _input = input;
            _regex = regex;
            Validate();
        }

        private void Validate()
        {
            if (!ReferenceEquals(_input.ResultType, typeof(string)))
            {
                ThrowCompileException(
                    CompileErrorResourceKeys.OperationNotDefinedForTypes,
                    CompileExceptionReason.TypeMismatch,
                    "MATCH",
                    _input.ResultType.Name,
                    typeof(Regex).Name);
            }
        }

        /// <summary>
        /// Emits <c>regex.IsMatch(input)</c> by loading the cached <see cref="Regex"/>
        /// followed by the input string.
        /// </summary>
        /// <param name="ilg">The IL generator.</param>
        /// <param name="services">The compile services.</param>
        public override void Emit(FleeILGenerator ilg, IServiceProvider services)
        {
            _regex.Emit(ilg, services);
            _input.Emit(ilg, services);
            ilg.Emit(OpCodes.Callvirt, _isMatchMethod);
        }

        /// <summary>
        /// Always <see cref="bool"/>: <c>MATCH</c> tests for a regex match.
        /// </summary>
        public override Type ResultType => typeof(bool);
    }
}
