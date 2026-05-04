using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the regex-based <c>MATCH</c> operator. The right-hand side must be a
    /// <c>/pattern/flags</c> regex literal — the contextual lexer enforces this.
    /// </summary>
    [TestClass]
    public class MatchOperatorTests
    {
        [TestMethod]
        public void Match_SimplePattern_Matches()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'abc123' MATCH /^[a-z]+\\d+$/");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Match_NoMatch_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'abc123' MATCH /^\\d+$/");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Match_CaseInsensitiveFlag_Matches()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'ADMIN' MATCH /^admin$/i");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Match_NoFlag_IsCaseSensitive()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'ADMIN' MATCH /^admin$/");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotMatch_NoMatch_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'abc' NOT MATCH /^\\d+$/");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotMatch_Match_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'abc123' NOT MATCH /\\d/");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Division_AfterIdentifier_StaysAsDivision()
        {
            // The lex gate must not turn `1 / 2 / 3` into a regex.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 / 2 / 3");

            Assert.AreEqual(0, e.Evaluate());
        }
    }
}
