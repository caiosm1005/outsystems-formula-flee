using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises literal parsing: numeric suffixes, real-number formats, boolean keywords,
    /// string escapes (both quoting styles), and the new date/time/regex literals.
    /// </summary>
    [TestClass]
    public class LiteralParsingTests
    {
        // --- Integer literals and suffixes ---

        [TestMethod]
        public void IntLiteral_NoSuffix_FitsInInt32()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1234567");

            Assert.AreEqual(1234567, e.Evaluate());
            Assert.AreEqual(typeof(int), e.Evaluate().GetType());
        }

        [TestMethod]
        public void IntLiteral_NoSuffix_AtInt32MaxStaysInt32()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("2147483647");

            Assert.AreEqual(int.MaxValue, e.Evaluate());
            Assert.AreEqual(typeof(int), e.Evaluate().GetType());
        }

        [TestMethod]
        public void LongSuffix_L_PromotesToInt64()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100L");

            Assert.AreEqual(100L, e.Evaluate());
            Assert.AreEqual(typeof(long), e.Evaluate().GetType());
        }

        [TestMethod]
        public void UnsignedSuffix_U_PromotesToUInt32()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100U");

            Assert.AreEqual(100U, e.Evaluate());
            Assert.AreEqual(typeof(uint), e.Evaluate().GetType());
        }

        [TestMethod]
        public void UnsignedLongSuffix_UL_PromotesToUInt64()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100UL");

            Assert.AreEqual(100UL, e.Evaluate());
            Assert.AreEqual(typeof(ulong), e.Evaluate().GetType());
        }

        [TestMethod]
        public void UnsignedLongSuffix_LU_AlsoWorks()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100LU");

            Assert.AreEqual(100UL, e.Evaluate());
        }

        // --- Real (floating-point) literals and suffixes ---

        [TestMethod]
        public void DoubleLiteral_NoSuffix_DefaultIsDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5");

            Assert.AreEqual(1.5, e.Evaluate());
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        [TestMethod]
        public void SingleLiteral_FSuffix_PromotesToSingle()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5f");

            Assert.AreEqual(1.5f, e.Evaluate());
            Assert.AreEqual(typeof(float), e.Evaluate().GetType());
        }

        [TestMethod]
        public void DoubleLiteral_DSuffix_StaysAsDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5d");

            Assert.AreEqual(1.5, e.Evaluate());
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        [TestMethod]
        public void DecimalLiteral_MSuffix_PromotesToDecimal()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5M");

            Assert.AreEqual(1.5M, e.Evaluate());
            Assert.AreEqual(typeof(decimal), e.Evaluate().GetType());
        }

        [TestMethod]
        public void RealLiteral_NoLeadingZero_AcceptedByDefault()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic(".25");

            Assert.AreEqual(0.25, e.Evaluate());
        }

        [TestMethod]
        public void RealLiteral_ScientificNotation_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.0e+3");

            Assert.AreEqual(1000.0, e.Evaluate());
        }

        [TestMethod]
        public void RealLiteral_NegativeExponent_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5e-2");

            Assert.AreEqual(0.015, e.Evaluate());
        }

        // --- Boolean ---

        [TestMethod]
        public void BoolLiteral_True_ParsesAsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("true");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void BoolLiteral_TitleCase_ParsesAsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("True");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void BoolLiteral_AllCaps_ParsesAsFalse()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("FALSE");

            Assert.AreEqual(false, e.Evaluate());
        }

        // --- String literals: double-quoted ---

        [TestMethod]
        public void StringLiteral_Empty_ParsesAsEmptyString()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"\"");

            Assert.AreEqual(string.Empty, e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_WithEscapedQuote_PreservesQuote()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"a\\\"b\"");

            Assert.AreEqual("a\"b", e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_WithUnicodeEscape_PreservesChar()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"a\\u0042c\"");

            Assert.AreEqual("aBc", e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_WithTabEscape_PreservesTab()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"a\\tb\"");

            Assert.AreEqual("a\tb", e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_DoubleBackslash_PreservesSingleBackslash()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"a\\\\b\"");

            Assert.AreEqual("a\\b", e.Evaluate());
        }

        // --- String literals: single-quoted (JS-style) ---

        [TestMethod]
        public void StringLiteral_SingleQuoted_Empty_ParsesAsEmptyString()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("''");

            Assert.AreEqual(string.Empty, e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_SingleQuoted_PlainText_ParsesAsString()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'hello'");

            Assert.AreEqual("hello", e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_SingleQuoted_EscapedApostrophe_PreservesApostrophe()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'don\\'t'");

            Assert.AreEqual("don't", e.Evaluate());
        }

        [TestMethod]
        public void StringLiteral_SingleQuoted_ContainsDoubleQuote_NoEscapeNeeded()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'say \"hi\"'");

            Assert.AreEqual("say \"hi\"", e.Evaluate());
        }

        // --- Date/Time/DateTime literals ---

        [TestMethod]
        public void DateLiteral_YearMonthDay_ParsesAsDateTime()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("#2026-05-04#");

            Assert.AreEqual(new System.DateTime(2026, 5, 4), e.Evaluate());
        }

        [TestMethod]
        public void DateLiteral_SingleDigitMonth_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("#2026-5-4#");

            Assert.AreEqual(new System.DateTime(2026, 5, 4), e.Evaluate());
        }

        [TestMethod]
        public void DateTimeLiteral_FullForm_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("#2026-05-04 12:30:45#");

            Assert.AreEqual(new System.DateTime(2026, 5, 4, 12, 30, 45), e.Evaluate());
        }

        [TestMethod]
        public void TimeLiteral_HoursMinutesSeconds_ParsesAsTimeSpan()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("#12:30:45#");

            Assert.AreEqual(new System.TimeSpan(12, 30, 45), e.Evaluate());
        }

        [TestMethod]
        public void TimeLiteral_SingleDigitHour_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("#5:6:7#");

            Assert.AreEqual(new System.TimeSpan(5, 6, 7), e.Evaluate());
        }

        [TestMethod]
        public void DateTime_Subtraction_ProducesTimeSpan()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("#2026-05-04# - #2026-05-03#");

            Assert.AreEqual(System.TimeSpan.FromDays(1), e.Evaluate());
        }
    }
}
