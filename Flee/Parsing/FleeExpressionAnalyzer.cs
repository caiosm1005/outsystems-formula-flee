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
    /// tracking, etc.).
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
        /// Folds an XOR sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The XOR production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitXorExpression(Production node)
        {
            AddBinaryOp(node, typeof(XorElement));
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
        /// Folds a shift sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The shift production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitShiftExpression(Production node)
        {
            AddBinaryOp(node, typeof(ShiftElement));
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
        /// Folds a power sub-expression into a binary-op element.
        /// </summary>
        /// <param name="node">The power production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitPowerExpression(Production node)
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

            // Get last child
            ExpressionElement childElement = (ExpressionElement)childValues[childValues.Count - 1]!;

            // Is it an signed integer constant?
            if (ReferenceEquals(childElement.GetType(), typeof(Int32LiteralElement)) & childValues.Count == 2)
            {
                ((Int32LiteralElement)childElement).Negate();
                // Add it directly instead of the negate element since it will already be negated
                node.AddValue(childElement);
            }
            else if (ReferenceEquals(childElement.GetType(), typeof(Int64LiteralElement)) & childValues.Count == 2)
            {
                ((Int64LiteralElement)childElement).Negate();
                // Add it directly instead of the negate element since it will already be negated
                node.AddValue(childElement);
            }
            else
            {
                // No so just add a regular negate
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
        /// Lifts the value produced by the special-function child to this node.
        /// </summary>
        /// <param name="node">The special-function-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitSpecialFunctionExpression(Production node)
        {
            AddFirstChildValue(node);
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
        /// Builds an <see cref="InElement"/> from a left-hand operand and either a literal
        /// list or a member-invocation list.
        /// </summary>
        /// <param name="node">The in-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitInExpression(Production node)
        {
            IList childValues = GetChildValues(node);

            if (childValues.Count == 1)
            {
                AddFirstChildValue(node);
                return node;
            }

            ExpressionElement operand = (ExpressionElement)childValues[0]!;
            childValues.RemoveAt(0);

            object second = childValues[0]!;
            InElement op;

            if (second is IList)
            {
                op = new InElement(operand, (IList)second);
            }
            else
            {
                InvocationListElement il = new(childValues, _myServices!);
                op = new InElement(operand, il);
            }

            node.AddValue(op);
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
        /// <see cref="ExitInExpression"/> can fork on it being a list.
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
        /// Builds a <see cref="CastElement"/> from the operand, target type parts, and
        /// array-flag produced by the cast-type sub-expression.
        /// </summary>
        /// <param name="node">The cast-expression production.</param>
        /// <returns>The same node.</returns>
        public override Node ExitCastExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            string[] destTypeParts = (string[])childValues[1]!;
            bool isArray = (bool)childValues[2]!;
            CastElement op = new((ExpressionElement)childValues[0]!, destTypeParts, isArray, _myServices!);
            node.AddValue(op);
            return node;
        }

        /// <summary>
        /// Splits the cast-type production into the type-name parts and an array-flag.
        /// </summary>
        /// <param name="node">The cast-type-expression production.</param>
        /// <returns>The same node, now carrying the parts and array flag.</returns>
        public override Node ExitCastTypeExpression(Production node)
        {
            IList childValues = GetChildValues(node);
            List<string> parts = [];

            foreach (string? part in childValues)
            {
                if (part != null)
                {
                    parts.Add(part);
                }
            }

            bool isArray = false;

            if (parts[parts.Count - 1] == "[]")
            {
                isArray = true;
                parts.RemoveAt(parts.Count - 1);
            }

            node.AddValue(parts.ToArray());
            node.AddValue(isArray);
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
            //string name = ((Token)node.GetChildAt(0))?.Image;
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
        /// Builds an <see cref="IntegralLiteralElement"/> from the matched hexadecimal-integer
        /// token.
        /// </summary>
        /// <param name="node">The hexadecimal-literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitHexLiteral(Token node)
        {
            LiteralElement element = IntegralLiteralElement.Create(node.Image, true, _myInUnaryNegate, _myServices!);
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
        /// to the string token.
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
        /// Builds a <see cref="CharLiteralElement"/>, applying escape-sequence processing to
        /// the character token.
        /// </summary>
        /// <param name="node">The character-literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitCharLiteral(Token node)
        {
            string s = DoEscapes(node.Image);
            node.AddValue(new CharLiteralElement(s[0]));
            return node;
        }

        /// <summary>
        /// Builds a <see cref="DateTimeLiteralElement"/> by stripping the leading and
        /// trailing <c>#</c> markers from the matched token.
        /// </summary>
        /// <param name="node">The date-time literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitDatetime(Token node)
        {
            ExpressionContext context = (ExpressionContext)_myServices!.GetService(typeof(ExpressionContext))!;
            string image = node.Image.Substring(1, node.Image.Length - 2);
            DateTimeLiteralElement element = new(image, context);
            node.AddValue(element);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="TimeSpanLiteralElement"/> by stripping the leading <c>##</c>
        /// and trailing <c>#</c> markers from the matched token.
        /// </summary>
        /// <param name="node">The time-span literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitTimespan(Token node)
        {
            string image = node.Image.Substring(2, node.Image.Length - 3);
            TimeSpanLiteralElement element = new(image);
            node.AddValue(element);
            return node;
        }

        private string DoEscapes(string image)
        {
            // Remove outer quotes
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
        /// Lifts the matched identifier text onto the token.
        /// </summary>
        /// <param name="node">The identifier token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitIdentifier(Token node)
        {
            node.AddValue(node.Image);
            return node;
        }

        /// <summary>
        /// Builds a <see cref="NullLiteralElement"/> for the <c>null</c> literal.
        /// </summary>
        /// <param name="node">The null-literal token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitNullLiteral(Token node)
        {
            node.AddValue(new NullLiteralElement());
            return node;
        }

        /// <summary>
        /// Carries up the literal <c>"[]"</c> string for the array-braces token, used by
        /// <see cref="ExitCastTypeExpression"/> to detect array casts.
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
        /// Records the modulo operation for the <c>%</c> token.
        /// </summary>
        /// <param name="node">The mod token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitMod(Token node)
        {
            node.AddValue(BinaryArithmeticOperation.Mod);
            return node;
        }

        /// <summary>
        /// Records the power operation for the <c>^</c> token.
        /// </summary>
        /// <param name="node">The power token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitPower(Token node)
        {
            node.AddValue(BinaryArithmeticOperation.Power);
            return node;
        }

        /// <summary>
        /// Records the equality operation for the <c>=</c> / <c>==</c> token.
        /// </summary>
        /// <param name="node">The eq token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitEq(Token node)
        {
            node.AddValue(LogicalCompareOperation.Equal);
            return node;
        }

        /// <summary>
        /// Records the inequality operation for the <c>&lt;&gt;</c> / <c>!=</c> token.
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
        /// Records the AND operation for the <c>and</c>/<c>&amp;&amp;</c> token.
        /// </summary>
        /// <param name="node">The and token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitAnd(Token node)
        {
            node.AddValue(AndOrOperation.And);
            return node;
        }

        /// <summary>
        /// Records the OR operation for the <c>or</c>/<c>||</c> token.
        /// </summary>
        /// <param name="node">The or token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitOr(Token node)
        {
            node.AddValue(AndOrOperation.Or);
            return node;
        }

        /// <summary>
        /// Records a sentinel <c>"Xor"</c> string for the XOR token.
        /// </summary>
        /// <param name="node">The xor token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitXor(Token node)
        {
            node.AddValue("Xor");
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
        /// Records the left-shift operation for the <c>&lt;&lt;</c> token.
        /// </summary>
        /// <param name="node">The left-shift token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitLeftShift(Token node)
        {
            node.AddValue(ShiftOperation.LeftShift);
            return node;
        }

        /// <summary>
        /// Records the right-shift operation for the <c>&gt;&gt;</c> token.
        /// </summary>
        /// <param name="node">The right-shift token.</param>
        /// <returns>The same token.</returns>
        public override Node ExitRightShift(Token node)
        {
            node.AddValue(ShiftOperation.RightShift);
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
