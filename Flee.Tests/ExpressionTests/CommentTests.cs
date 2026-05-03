using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    [TestClass]
    public class CommentTests
    {
        // --- Single-line comments ---

        [TestMethod]
        public void SingleLineComment_AtEndOfExpression_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 2 // adds them");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void SingleLineComment_OnlyConsumesUpToNewline_ExpressionContinuesAfter()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 2 // first line\n + 3");

            Assert.AreEqual(6, e.Evaluate());
        }

        [TestMethod]
        public void SingleLineComment_BeforeExpression_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("// header\n42");

            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void SingleLineComment_DoesNotInterfereWithDivisionOperator()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 / 2");

            Assert.AreEqual(5, e.Evaluate());
        }

        [TestMethod]
        public void SingleLineComment_LongerThanDivision_TakesPrecedence()
        {
            // Without `//` recognition this would tokenize as `5 / / 2` and fail to parse.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("5 //2");

            Assert.AreEqual(5, e.Evaluate());
        }

        [TestMethod]
        public void SingleLineComment_InsideStringLiteral_IsNotTreatedAsComment()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"// not a comment\"");

            Assert.AreEqual("// not a comment", e.Evaluate());
        }

        // --- Multi-line comments ---

        [TestMethod]
        public void MultiLineComment_BetweenTokens_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + /* comment */ 2");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_SpanningMultipleLines_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + /* line one\nline two\nline three */ 2");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_AtStartOfExpression_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("/* leading */ 42");

            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_AtEndOfExpression_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("42 /* trailing */");

            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_Empty_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 +/**/2");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_WithAsterisksInBody_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + /* a * b * c */ 2");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_DocStyleWithMultipleAsterisks_IsIgnored()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + /** doc **/ 2");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_InsideStringLiteral_IsNotTreatedAsComment()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"/* not a comment */\"");

            Assert.AreEqual("/* not a comment */", e.Evaluate());
        }

        [TestMethod]
        public void MultiLineComment_DoesNotNest_FirstClosingTerminatesOuter()
        {
            // C-style comments do not nest. The first */ closes the comment,
            // leaving "+ 2" to be parsed as part of the expression.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 /* outer /* inner */ + 2");

            Assert.AreEqual(3, e.Evaluate());
        }

        // --- Combined ---

        [TestMethod]
        public void MixedSingleAndMultiLineComments_AreAllIgnored()
        {
            var context = new ExpressionContext();
            var script = "// header\n" +
                         "1 /* a */ + /* b */ 2 // trailing\n" +
                         "+ 3 /* end */";
            var e = context.CompileDynamic(script);

            Assert.AreEqual(6, e.Evaluate());
        }

        // --- Calc engine integration ---

        [TestMethod]
        public void CalcEngine_AcceptsScriptsContainingComments()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            context.Variables.Add("x", 10);

            ce.Add("a", "x * 2 // double x", context);
            ce.Add("b", "a /* depends on a */ + 1", context);
            ce.Recalculate("a");

            Assert.AreEqual(21, ce.GetResult<int>("b"));
        }
    }
}
