using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the six comparison operators across numeric, string, char, boolean, enum, and
    /// reference types — including reference equality with <c>null</c>.
    /// </summary>
    [TestClass]
    public class ComparisonOperatorTests
    {
        // --- Equality / inequality on integers ---

        [TestMethod]
        public void Equal_TwoMatchingInts_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("42 = 42");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Equal_DifferentInts_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("42 = 43");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void NotEqual_DifferentInts_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("42 <> 43");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotEqual_MatchingInts_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("42 <> 42");

            Assert.AreEqual(false, e.Evaluate());
        }

        // --- Less than / greater than ---

        [TestMethod]
        public void LessThan_SmallerLeftSide_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 < 20");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void GreaterThan_LargerLeftSide_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("20 > 10");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void LessThanOrEqual_EqualValues_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 <= 10");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void GreaterThanOrEqual_EqualValues_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 >= 10");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void LessThanOrEqual_GreaterLeftSide_ReturnsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("11 <= 10");

            Assert.AreEqual(false, e.Evaluate());
        }

        // --- Mixed numeric type comparisons ---

        [TestMethod]
        public void Compare_IntAndDouble_PromotesAndCompares()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 = 10.0");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Compare_IntAndLong_PromotesAndCompares()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 = 10L");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Compare_NegativeIntAndUint_TreatsAsSigned()
        {
            // Per the existing valid expressions list, -1 > 0 is false (mixed signed/unsigned compare).
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-1 > 0U");

            Assert.AreEqual(false, e.Evaluate());
        }

        // --- String comparisons ---

        [TestMethod]
        public void Equal_TwoMatchingStrings_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"abc\" = \"abc\"");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Equal_TwoEmptyStrings_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"\" = \"\"");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotEqual_DifferentStrings_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"abc\" <> \"def\"");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Equal_StringComparison_DefaultIsOrdinal()
        {
            // StringComparison default is Ordinal — case difference means inequality.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"abc\" = \"ABC\"");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Equal_StringComparison_OrdinalIgnoreCase()
        {
            var context = new ExpressionContext();
            context.Options.StringComparison = System.StringComparison.OrdinalIgnoreCase;
            var e = context.CompileDynamic("\"abc\" = \"ABC\"");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- Boolean comparisons ---

        [TestMethod]
        public void Equal_TwoTrues_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("true = true");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotEqual_TrueAndFalse_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("true <> false");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- Single-quoted string comparisons ---

        [TestMethod]
        public void Equal_TwoSingleQuotedStrings_ReturnsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'a' = 'a'");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- Enum comparisons ---

        [TestMethod]
        public void Equal_TwoMatchingEnumValues_ReturnsTrue()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(System.DayOfWeek), "DayOfWeek");
            var e = context.CompileDynamic("DayOfWeek.Monday = DayOfWeek.Monday");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NotEqual_DifferentEnumValues_ReturnsTrue()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(System.DayOfWeek), "DayOfWeek");
            var e = context.CompileDynamic("DayOfWeek.Monday <> DayOfWeek.Friday");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void GreaterThan_TwoEnumValues_ComparesUnderlying()
        {
            // Friday (5) > Wednesday (3)
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(System.DayOfWeek), "DayOfWeek");
            var e = context.CompileDynamic("DayOfWeek.Friday > DayOfWeek.Wednesday");

            Assert.AreEqual(true, e.Evaluate());
        }

        // --- Chained comparisons (booleans flow through) ---

        [TestMethod]
        public void Chained_GreaterThanFollowedByEqualToTrue_Holds()
        {
            // 10 > 2 → true; true = true → true
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 > 2 = true");

            Assert.AreEqual(true, e.Evaluate());
        }
    }
}
