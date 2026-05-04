using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the SQL-style <c>LIKE</c> operator. <c>LIKE</c> is always
    /// case-insensitive to match SQL Server / OutSystems semantics.
    /// </summary>
    [TestClass]
    public class LikeOperatorTests
    {
        [TestMethod]
        public void Like_PercentWildcard_MatchesPrefix()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello world' LIKE 'hello%'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Like_PercentWildcard_MatchesSuffix()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello world' LIKE '%world'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Like_UnderscoreWildcard_MatchesSingleChar()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello' LIKE 'h_l_o'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Like_CharClass_MatchesAnyOfClass()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'cat' LIKE '[bc]at'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Like_NegatedCharClass_DoesNotMatch()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'cat' LIKE '[^c]at'");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Like_CaseInsensitive_MatchesDifferentCase()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'Hello' LIKE 'hello'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Like_NoWildcards_RequiresExactMatch()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello' LIKE 'help'");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotLike_NoMatch_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello' NOT LIKE '%test%'");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotLike_Match_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello' NOT LIKE 'h%'");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Like_VariablePattern_CompilesAtRuntime()
        {
            var context = new ExpressionContext();
            context.Variables.Add("input", "hello world");
            context.Variables.Add("pattern", "hello%");
            var e = context.CompileDynamic("input LIKE pattern");

            Assert.AreEqual(true, e.Evaluate());
        }
    }
}
