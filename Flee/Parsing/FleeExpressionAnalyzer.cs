using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Flee.ExpressionElements;
using Flee.ExpressionElements.Base;
using Flee.ExpressionElements.Base.Literals;
using Flee.ExpressionElements.Literals;
using Flee.ExpressionElements.Literals.Integral;
using Flee.ExpressionElements.LogicalBitwise;
using Flee.ExpressionElements.MemberElements;
using Flee.InternalTypes;
using Flee.PublicTypes;

namespace Flee.Parsing
{
    /// <summary>
    /// Subclass of the Grammatica-generated <see cref="ExpressionAnalyzer"/> that walks the
    /// parse tree and produces <see cref="ExpressionElement"/> nodes. Adds Flee-specific
    /// behavior on top of the generated visitor (services, escape handling, unary-negate
    /// tracking, SQL-op dispatch, etc.).
    /// </summary>
    internal class FleeExpressionAnalyzer : ExpressionAnalyzer
    {
        private IServiceProvider? _myServices;
        private readonly Regex _myUnicodeEscapeRegex;
        private readonly Regex _myRegularEscapeRegex;

        private bool _myInUnaryNegate;

        /// <summary>
        /// Initializes a new instance, pre-compiling the escape-sequence regexes used by
        /// string-literal handling.
        /// </summary>
        internal FleeExpressionAnalyzer()
        {
            _myUnicodeEscapeRegex = new Regex("\\\\u[0-9a-f]{4}", RegexOptions.IgnoreCase);
            _myRegularEscapeRegex = new Regex("\\\\[\\\\\"'trn]", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Sets the service provider used to resolve Flee services during analysis.
        /// </summary>
        /// <param name="services">The service provider for the current expression.</param>
        public void SetServices(IServiceProvider services)
        {
            _myServices = services;
        }

        /// <summary>
        /// Clears the cached service provider so the analyzer can be reused for the next
        /// parse.
        /// </summary>
        public override void Reset()
        {
            _myServices = null;
        }

        /// <summary>
        /// Lifts the value of the first child to the expression node.
        /// </summary>
        /// <param name="node">The expression production.</param>
        /// <returns>The same node, now carrying the lifted value.</returns>
        public override Node ExitExpression(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Forwards every child value of the expression-group production to the node.
        /// </summary>
        /// <param name="node">The expression-group production.</param>
        /// <returns>The same node, now carrying every child value.</returns>
        public override Node ExitExpressionGroup(Production node)
        {
            node.AddValues(GetChildValues(node));
            return node;
        }

        /// <summary>
        /// Folds an OR sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The OR production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitOrExpression(Production node)
        {
            AddBinaryOp(node, typeof(AndOrElement));
            return node;
        }

        /// <summary>
        /// Folds an AND sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The AND production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitAndExpression(Production node)
        {
            AddBinaryOp(node, typeof(AndOrElement));
            return node;
        }

        /// <summary>
        /// Folds a NOT sub-expression into a unary-op element.
        /// </summary>
        /// <param name="node">The NOT production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitNotExpression(Production node)
        {
            AddUnaryOp(node, typeof(NotElement));
            return node;
        }

        /// <summary>
        /// Folds a comparison sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The comparison production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitCompareExpression(Production node)
        {
            AddBinaryOp(node, typeof(CompareElement));
            return node;
        }

        /// <summary>
        /// Folds an additive sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The additive production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitAdditiveExpression(Production node)
        {
            AddBinaryOp(node, typeof(ArithmeticElement));
            return node;
        }

        /// <summary>
        /// Folds a multiplicative sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The multiplicative production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitMultiplicativeExpression(Production node)
        {
            AddBinaryOp(node, typeof(ArithmeticElement));
            return node;
        }

        /// <summary>
        /// Folds a unary-negate sub-expression into either a negated literal (for integer
        /// constants such as <c>int.MinValue</c>) or a regular <see cref="NegateElement"/>.
        /// </summary>
        /// <param name="node">The negate production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitNegateExpression(Production node)
        {
            IList childValues = GetChildValues(node);

            ExpressionElement childElement = (ExpressionElement)childValues[childValues.Count - 1]!;

            if (ReferenceEquals(childElement.GetType(), typeof(Int32LiteralElement)) & childValues.Count == 2)
            {
                ((Int32LiteralElement)childElement).Negate();
                node.AddValue(childElement);
            }
            else if (ReferenceEquals(childElement.GetType(), typeof(Int64LiteralElement)) & childValues.Count == 2)
            {
                ((Int64LiteralElement)childElement).Negate();
                node.AddValue(childElement);
            }
            else
            {
                AddUnaryOp(node, typeof(NegateElement));
            }

            return node;
        }

        /// <summary>
        /// Builds the member-access chain represented by this production, wrapping it in an
        /// <see cref="InvocationListElement"/> when more than the bare identifier appears.
        /// </summary>
        /// <param name="node">The member-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitMemberExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            object first = childValues[0]!;

            if (childValues.Count == 1 && first is not MemberElement)
            {
                node.AddValue(first);
            }
            else
            {
                InvocationListElement list = new(childValues, _myServices!);
                node.AddValue(list);
            }

            return node;
        }

        /// <summary>
        /// Wraps the index arguments in an <see cref="IndexerElement"/>.
        /// </summary>
        /// <param name="node">The index-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitIndexExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            ArgumentList args = new(childValues);
            IndexerElement e = new(args);
            node.AddValue(e);
            return node;
        }

        /// <summary>
        /// Carries up the value produced by the right-hand member identifier.
        /// </summary>
        /// <param name="node">The member-access production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitMemberAccessExpression(Production node)
        {
            node.AddValue(node.GetChildAt(1)!.GetValue(0));
            return node;
        }

        /// <summary>
        /// Builds a ternary <see cref="ConditionalElement"/> (the <c>if(cond, then, else)</c>
        /// builtin).
        /// </summary>
        /// <param name="node">The if-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitIfExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            ConditionalElement op = new(
                (ExpressionElement)childValues[0]!,
                (ExpressionElement)childValues[1]!,
                (ExpressionElement)childValues[2]!);
            node.AddValue(op);
            return node;
        }

        /// <summary>
        /// Combines the left-hand <see cref="CompareExpression"/> with an optional SQL-op
        /// builder produced by the right-hand <c>SqlOpRhs</c>.
        /// </summary>
        /// <param name="node">The sql-op-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitSqlOpExpression(Production node)
        {
            IList childValues = GetChildValues(node);

            if (childValues.Count == 1)
            {
                AddFirstChildValue(node);
                return node;
            }

            ExpressionElement operand = (ExpressionElement)childValues[0]!;
            Func<ExpressionElement, ExpressionElement> builder =
                (Func<ExpressionElement, ExpressionElement>)childValues[1]!;
            node.AddValue(builder(operand));
            return node;
        }

        /// <summary>
        /// Lifts the inner builder from the chosen alternative, wrapping it in a NOT when the
        /// negated branch fires.
        /// </summary>
        /// <param name="node">The sql-op-rhs production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitSqlOpRhs(Production node)
        {
            IList childValues = GetChildValues(node);
            // NOT branch flattens a synthetic subproduction whose children are
            // [NOT_token (string.Empty), inner builder].
            if (childValues.Count == 2)
            {
                Func<ExpressionElement, ExpressionElement> inner =
                    (Func<ExpressionElement, ExpressionElement>)childValues[1]!;
                node.AddValue(WrapWithNot(inner));
                return node;
            }
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Lifts the inner builder produced by the chosen alternative.
        /// </summary>
        /// <param name="node">The negated-sql-op-rhs production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitNegatedSqlOpRhs(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Builds the <c>IN</c> builder. Returns a <see cref="Func{T,TResult}"/> that, given
        /// the operand from <see cref="ExitSqlOpExpression"/>, produces an
        /// <see cref="InElement"/>.
        /// </summary>
        /// <param name="node">The in-rhs production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitInRhs(Production node)
        {
            IList childValues = GetChildValues(node);
            object target = childValues[0]!;
            Func<ExpressionElement, ExpressionElement> builder = operand =>
            {
                if (target is IList list)
                {
                    return new InElement(operand, list);
                }
                List<object?> chain = [target];
                InvocationListElement il = new(chain, _myServices!);
                return new InElement(operand, il);
            };
            node.AddValue(builder);
            return node;
        }

        /// <summary>
        /// Builds the <c>LIKE</c> builder.
        /// </summary>
        /// <param name="node">The like-rhs production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitLikeRhs(Production node)
        {
            IList childValues = GetChildValues(node);
            ExpressionElement pattern = (ExpressionElement)childValues[0]!;
            Func<ExpressionElement, ExpressionElement> builder = operand => new LikeElement(operand, pattern);
            node.AddValue(builder);
            return node;
        }

        /// <summary>
        /// Builds the <c>MATCH</c> builder. The right-hand side is a REGEXP token whose
        /// <see cref="ExitRegexp"/> hook produced a <see cref="RegexLiteralElement"/>.
        /// </summary>
        /// <param name="node">The match-rhs production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitMatchRhs(Production node)
        {
            IList childValues = GetChildValues(node);
            RegexLiteralElement regex = (RegexLiteralElement)childValues[0]!;
            Func<ExpressionElement, ExpressionElement> builder = operand => new MatchElement(operand, regex);
            node.AddValue(builder);
            return node;
        }

        /// <summary>
        /// Builds the <c>CONTAINS</c> builder. Inspects the contains-target shape to decide
        /// which <see cref="ContainsElement"/> overload to construct.
        /// </summary>
        /// <param name="node">The contains-rhs production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitContainsRhs(Production node)
        {
            IList childValues = GetChildValues(node);
            object target = childValues[0]!;
            Func<ExpressionElement, ExpressionElement> builder = collection =>
                ContainsElement.Build(collection, target, _myServices!);
            node.AddValue(builder);
            return node;
        }

        /// <summary>
        /// Lifts the contains-target value (quantifier wrapper, regex literal, or plain
        /// expression) so <see cref="ExitContainsRhs"/> can inspect it.
        /// </summary>
        /// <param name="node">The contains-target-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitContainsTargetExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            // ANY / ALL branches flatten a synthetic subproduction so the children are
            // [ANY_token (string.Empty), arg list]. Wrap as a quantifier marker.
            if (childValues.Count == 2)
            {
                ContainsQuantifier quantifier = node.GetChildAt(0)!.GetValue(0) is string s && s == "ALL"
                    ? ContainsQuantifier.All
                    : ContainsQuantifier.Any;
                node.AddValue(new ContainsElement.QuantifiedTarget(quantifier, childValues[1]!));
            }
            else
            {
                AddFirstChildValue(node);
            }
            return node;
        }

        /// <summary>
        /// Lifts the contains-arg-list value. A bare member reference (the <c>MyArr2</c> in
        /// <c>CONTAINS ANY MyArr2</c>) is wrapped in an <see cref="InvocationListElement"/> so
        /// its <see cref="ExpressionElement.ResultType"/> resolves at compile time — mirroring
        /// what <see cref="ExitInRhs"/> does for the parenthesised IN target. The literal-list
        /// branch (already an <see cref="IList"/>) is forwarded unchanged.
        /// </summary>
        /// <param name="node">The contains-arg-list production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitContainsArgList(Production node)
        {
            IList childValues = GetChildValues(node);
            object first = childValues[0]!;
            if (first is ExpressionElement)
            {
                List<object?> chain = [first];
                InvocationListElement il = new(chain, _myServices!);
                node.AddValue(il);
            }
            else
            {
                node.AddValue(first);
            }
            return node;
        }

        /// <summary>
        /// Lifts the value produced by the in-target child to this node.
        /// </summary>
        /// <param name="node">The in-target-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitInTargetExpression(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Stores the list of in-target values directly on the node so the parent
        /// <see cref="ExitInRhs"/> can fork on it being a list.
        /// </summary>
        /// <param name="node">The in-list-target-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitInListTargetExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            node.AddValue(childValues);
            return node;
        }

        /// <summary>
        /// Lifts the value produced by the member-function child to this node.
        /// </summary>
        /// <param name="node">The member-function-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitMemberFunctionExpression(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Builds an <see cref="IdentifierElement"/> from the field/property name.
        /// </summary>
        /// <param name="node">The field-property-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitFieldPropertyExpression(Production node)
        {
            string name = node.GetChildAt(0)!.GetValue(0).ToString()!;
            IdentifierElement elem = new(name);
            node.AddValue(elem);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="FunctionCallElement"/> from the function name and argument
        /// list.
        /// </summary>
        /// <param name="node">The function-call-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitFunctionCallExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            string name = (string)childValues[0]!;
            childValues.RemoveAt(0);
            ArgumentList args = new(childValues);
            FunctionCallElement funcCall = new(name, args);
            node.AddValue(funcCall);
            return node;
        }

        /// <summary>
        /// Forwards every argument value collected by the argument-list production to the
        /// node.
        /// </summary>
        /// <param name="node">The argument-list production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitArgumentList(Production node)
        {
            IList childValues = GetChildValues(node);
            node.AddValues((ArrayList)childValues);
            return node;
        }

        /// <summary>
        /// Lifts the value produced by the basic-expression child to this node.
        /// </summary>
        /// <param name="node">The basic-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitBasicExpression(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Lifts the value produced by the literal-expression child to this node.
        /// </summary>
        /// <param name="node">The literal-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitLiteralExpression(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        private void AddFirstChildValue(Production node)
        {
            node.AddValue(GetChildAt(node, 0).Values[0]!);
        }

        private void AddUnaryOp(Production node, Type elementType)
        {
            IList childValues = GetChildValues(node);

            if (childValues.Count == 2)
            {
                UnaryElement element = (UnaryElement)Activator.CreateInstance(elementType)!;
                element.SetChild((ExpressionElement)childValues[1]!);
                node.AddValue(element);
            }
            else
            {
                node.AddValue(childValues[0]!);
            }
        }

        private void AddBinaryOp(Production node, Type elementType)
        {
            IList childValues = GetChildValues(node);

            if (childValues.Count > 1)
            {
                BinaryExpressionElement e = BinaryExpressionElement.CreateElement(childValues, elementType);
                node.AddValue(e);
            }
            else if (childValues.Count == 1)
            {
                node.AddValue(childValues[0]!);
            }
            else
            {
                Debug.Assert(false, "wrong number of chilren");
            }
        }

        private static Func<ExpressionElement, ExpressionElement> WrapWithNot(
            Func<ExpressionElement, ExpressionElement> inner)
        {
            return operand =>
            {
                ExpressionElement built = inner(operand);
                NotElement notElem = new();
                notElem.SetChild(built);
                return notElem;
            };
        }

        /// <summary>
        /// Builds a <see cref="RealLiteralElement"/> from the matched real-number token.
        /// </summary>
        /// <param name="node">The real token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitReal(Token node)
        {
            string image = node.Image;
            LiteralElement element = RealLiteralElement.Create(image, _myServices!);

            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Builds an <see cref="IntegralLiteralElement"/> from the matched decimal-integer
        /// token.
        /// </summary>
        /// <param name="node">The integer token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitInteger(Token node)
        {
            LiteralElement element = IntegralLiteralElement.Create(node.Image, false, _myInUnaryNegate, _myServices!);
            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Lifts the value produced by the boolean-literal child to this node.
        /// </summary>
        /// <param name="node">The boolean-literal-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitBooleanLiteralExpression(Production node)
        {
            AddFirstChildValue(node);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="BooleanLiteralElement"/> for the literal <c>true</c>.
        /// </summary>
        /// <param name="node">The true token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitTrue(Token node)
        {
            node.AddValue(new BooleanLiteralElement(true));
            return node;
        }

        /// <summary>
        /// Builds a <see cref="BooleanLiteralElement"/> for the literal <c>false</c>.
        /// </summary>
        /// <param name="node">The false token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitFalse(Token node)
        {
            node.AddValue(new BooleanLiteralElement(false));
            return node;
        }

        /// <summary>
        /// Builds a <see cref="StringLiteralElement"/>, applying escape-sequence processing
        /// to the string token. Accepts both double-quoted (<c>"..."</c>) and single-quoted
        /// (<c>'...'</c>) literals; the leading and trailing delimiter is stripped before
        /// escape decoding.
        /// </summary>
        /// <param name="node">The string-literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitStringLiteral(Token node)
        {
            string s = DoEscapes(node.Image);
            StringLiteralElement element = new(s);
            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="DateTimeLiteralElement"/> by stripping the leading and
        /// trailing <c>#</c> markers from the matched token and parsing
        /// <c>yyyy-M-d H:m:s</c>.
        /// </summary>
        /// <param name="node">The date-time literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitDatetime(Token node)
        {
            string image = node.Image.Substring(1, node.Image.Length - 2);
            DateTimeLiteralElement element = new(image);
            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="DateLiteralElement"/> by stripping the leading and trailing
        /// <c>#</c> markers from the matched token and parsing <c>yyyy-M-d</c>.
        /// </summary>
        /// <param name="node">The date literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitDate(Token node)
        {
            string image = node.Image.Substring(1, node.Image.Length - 2);
            DateLiteralElement element = new(image);
            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="TimeLiteralElement"/> by stripping the leading and trailing
        /// <c>#</c> markers from the matched token and parsing <c>H:m:s</c>.
        /// </summary>
        /// <param name="node">The time literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitTime(Token node)
        {
            string image = node.Image.Substring(1, node.Image.Length - 2);
            TimeLiteralElement element = new(image);
            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="RegexLiteralElement"/> from the JS-style <c>/pattern/flags</c>
        /// token (the leading and trailing <c>/</c> and any flag letters are part of
        /// <see cref="Token.Image"/>).
        /// </summary>
        /// <param name="node">The regex token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitRegexp(Token node)
        {
            RegexLiteralElement element = new(node.Image);
            node.AddValue(element);
            return node;
        }

        private string DoEscapes(string image)
        {
            // Remove outer quotes (works for both '...' and "...")
            image = image.Substring(1, image.Length - 2);
            image = _myUnicodeEscapeRegex.Replace(image, UnicodeEscapeMatcher);
            image = _myRegularEscapeRegex.Replace(image, RegularEscapeMatcher);
            return image;
        }

        private string RegularEscapeMatcher(Match m)
        {
            string s = m.Value;
            // Remove leading \
            s = s.Substring(1);

            switch (s)
            {
                case "\\":
                case "\"":
                case "'":
                    return s;
                case "t":
                case "T":
                    return Convert.ToChar(9).ToString();
                case "n":
                case "N":
                    return Convert.ToChar(10).ToString();
                case "r":
                case "R":
                    return Convert.ToChar(13).ToString();
                default:
                    Debug.Assert(false, "Unrecognized escape sequence");
                    return string.Empty;
            }
        }

        private string UnicodeEscapeMatcher(Match m)
        {
            string s = m.Value;
            // Remove \u
            s = s.Substring(2);
            int code = int.Parse(s, NumberStyles.AllowHexSpecifier);
            char c = Convert.ToChar(code);
            return c.ToString();
        }

        /// <summary>
        /// Lifts the matched identifier text onto the token. The verbatim image — including
        /// any leading <c>@</c> or <c>@@</c> prefix — is used as the variable lookup key.
        /// </summary>
        /// <param name="node">The identifier token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitIdentifier(Token node)
        {
            node.AddValue(node.Image);
            return node;
        }

        /// <summary>
        /// Carries up the literal <c>"[]"</c> string for the array-braces token.
        /// </summary>
        /// <param name="node">The array-braces token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitArrayBraces(Token node)
        {
            node.AddValue("[]");
            return node;
        }

        /// <summary>
        /// Records the addition operation for the <c>+</c> token.
        /// </summary>
        /// <param name="node">The add token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitAdd(Token node)
        {
            node.AddValue(BinaryArithmeticOperation.Add);
            return node;
        }

        /// <summary>
        /// Records the subtraction operation for the <c>-</c> token.
        /// </summary>
        /// <param name="node">The sub token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitSub(Token node)
        {
            node.AddValue(BinaryArithmeticOperation.Subtract);
            return node;
        }

        /// <summary>
        /// Records the multiplication operation for the <c>*</c> token.
        /// </summary>
        /// <param name="node">The mul token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitMul(Token node)
        {
            node.AddValue(BinaryArithmeticOperation.Multiply);
            return node;
        }

        /// <summary>
        /// Records the division operation for the <c>/</c> token.
        /// </summary>
        /// <param name="node">The div token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitDiv(Token node)
        {
            node.AddValue(BinaryArithmeticOperation.Divide);
            return node;
        }

        /// <summary>
        /// Records the equality operation for the <c>=</c> token.
        /// </summary>
        /// <param name="node">The eq token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitEq(Token node)
        {
            node.AddValue(LogicalCompareOperation.Equal);
            return node;
        }

        /// <summary>
        /// Records the inequality operation for the <c>&lt;&gt;</c> token.
        /// </summary>
        /// <param name="node">The ne token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitNe(Token node)
        {
            node.AddValue(LogicalCompareOperation.NotEqual);
            return node;
        }

        /// <summary>
        /// Records the less-than operation for the <c>&lt;</c> token.
        /// </summary>
        /// <param name="node">The lt token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitLt(Token node)
        {
            node.AddValue(LogicalCompareOperation.LessThan);
            return node;
        }

        /// <summary>
        /// Records the greater-than operation for the <c>&gt;</c> token.
        /// </summary>
        /// <param name="node">The gt token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitGt(Token node)
        {
            node.AddValue(LogicalCompareOperation.GreaterThan);
            return node;
        }

        /// <summary>
        /// Records the less-than-or-equal operation for the <c>&lt;=</c> token.
        /// </summary>
        /// <param name="node">The lte token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitLte(Token node)
        {
            node.AddValue(LogicalCompareOperation.LessThanOrEqual);
            return node;
        }

        /// <summary>
        /// Records the greater-than-or-equal operation for the <c>&gt;=</c> token.
        /// </summary>
        /// <param name="node">The gte token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitGte(Token node)
        {
            node.AddValue(LogicalCompareOperation.GreaterThanOrEqual);
            return node;
        }

        /// <summary>
        /// Records the AND operation for the <c>AND</c> token.
        /// </summary>
        /// <param name="node">The and token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitAnd(Token node)
        {
            node.AddValue(AndOrOperation.And);
            return node;
        }

        /// <summary>
        /// Records the OR operation for the <c>OR</c> token.
        /// </summary>
        /// <param name="node">The or token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitOr(Token node)
        {
            node.AddValue(AndOrOperation.Or);
            return node;
        }

        /// <summary>
        /// Records an empty placeholder value for the NOT token.
        /// </summary>
        /// <param name="node">The not token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitNot(Token node)
        {
            node.AddValue(string.Empty);
            return node;
        }

        /// <summary>
        /// Records the literal text "ANY" for the quantifier token, used by
        /// <see cref="ExitContainsTargetExpression"/> to pick the quantifier.
        /// </summary>
        /// <param name="node">The any token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitAny(Token node)
        {
            node.AddValue("ANY");
            return node;
        }

        /// <summary>
        /// Records the literal text "ALL" for the quantifier token, used by
        /// <see cref="ExitContainsTargetExpression"/> to pick the quantifier.
        /// </summary>
        /// <param name="node">The all token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitAll(Token node)
        {
            node.AddValue("ALL");
            return node;
        }

        /// <summary>
        /// Tracks whether the analyzer is currently inside a unary-negate production whose
        /// child is a literal <c>-</c>; this lets the integer-literal builder produce the
        /// correctly-signed value (handling <c>int.MinValue</c>).
        /// </summary>
        /// <param name="node">The parent production.</param>
        /// <param name="child">The child being attached.</param>
        public override void Child(Production node, Node child)
        {
            base.Child(node, child);
            _myInUnaryNegate = node.Id == (int)ExpressionConstants.NEGATE_EXPRESSION
                & child.Id == (int)ExpressionConstants.SUB;
        }
    }
}
