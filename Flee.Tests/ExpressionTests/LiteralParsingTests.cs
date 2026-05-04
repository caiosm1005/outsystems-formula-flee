using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises literal parsing: numeric suffixes, hex literals, char escapes, string escapes,
    /// boolean keywords, and the null literal.
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
            // Both UL and LU are accepted.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100LU");

            Assert.AreEqual(100UL, e.Evaluate());
        }

        // --- Hex literals ---

        [TestMethod]
        public void HexLiteral_Lowercase_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("0xff");

            Assert.AreEqual(255, e.Evaluate());
        }

        [TestMethod]
        public void HexLiteral_MixedCase_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("0xDeaD");

            Assert.AreEqual(0xDEAD, e.Evaluate());
        }

        [TestMethod]
        public void HexLiteral_NegativePrefix_ParsesAsNegated()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-0xA");

            Assert.AreEqual(-10, e.Evaluate());
        }

        [TestMethod]
        public void HexLiteral_WithLongSuffix_PromotesToLong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("0xDeaDL");

            Assert.AreEqual(0xDEADL, e.Evaluate());
            Assert.AreEqual(typeof(long), e.Evaluate().GetType());
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
            // ExpressionParserOptions.RequireDigitsBeforeDecimalPoint defaults to false.
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
            // CaseSensitive default is false — keyword case shouldn't matter.
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

        // --- Char literals ---

        [TestMethod]
        public void CharLiteral_PlainAscii_ParsesAsChar()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'a'");

            Assert.AreEqual('a', e.Evaluate());
            Assert.AreEqual(typeof(char), e.Evaluate().GetType());
        }

        [TestMethod]
        public void CharLiteral_EscapedQuote_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'\\''");

            Assert.AreEqual('\'', e.Evaluate());
        }

        [TestMethod]
        public void CharLiteral_EscapedBackslash_ParsesCorrectly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'\\\\'");

            Assert.AreEqual('\\', e.Evaluate());
        }

        [TestMethod]
        public void CharLiteral_UnicodeEscape_ParsesCorrectly()
        {
            // ^ = '^'
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'\\u005E'");

            Assert.AreEqual('^', e.Evaluate());
        }

        [TestMethod]
        public void CharLiteral_TabEscape_ParsesAsTab()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("'\\t'");

            Assert.AreEqual('\t', e.Evaluate());
        }

        // --- String literals ---

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
            // B = 'B'
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

        // --- Null literal ---

        [TestMethod]
        public void NullLiteral_StandaloneEqualsNull_IsTrue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("null = null");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void NullLiteral_StringConcat_TreatsNullAsEmpty()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"abc\" + null");

            Assert.AreEqual("abc", e.Evaluate());
        }

        // --- DateTime / TimeSpan literals ---

        /// <summary>
        /// Builds a context with an explicit DateTime parse format so tests are not affected
        /// by the ambient system culture.
        /// </summary>
        private static ExpressionContext NewDateContext(string format = "dd/MM/yyyy")
        {
            var context = new ExpressionContext();
            context.ParserOptions.DateTimeFormat = format;
            context.ParserOptions.RecreateParser();
            return context;
        }

        [TestMethod]
        public void DateTimeLiteral_DayMonthYearFormat_ParsesCorrectly()
        {
            // Use an unambiguous date — only valid as dd/MM/yyyy.
            var context = NewDateContext();
            var e = context.CompileDynamic("#31/12/2008#");

            Assert.AreEqual(new System.DateTime(2008, 12, 31), e.Evaluate());
        }

        [TestMethod]
        public void TimeSpanLiteral_HoursMinutes_ParsesCorrectly()
        {
            var context = NewDateContext();
            var e = context.CompileDynamic("##23:45#");

            Assert.AreEqual(new System.TimeSpan(23, 45, 0), e.Evaluate());
        }

        [TestMethod]
        public void TimeSpanLiteral_DaysHoursMinutesSeconds_ParsesCorrectly()
        {
            var context = NewDateContext();
            var e = context.CompileDynamic("##12.23:45:11#");

            Assert.AreEqual(new System.TimeSpan(12, 23, 45, 11), e.Evaluate());
        }

        [TestMethod]
        public void DateTime_Subtraction_ProducesTimeSpan()
        {
            var context = NewDateContext();
            var e = context.CompileDynamic("#15/12/2008# - #14/12/2008#");

            Assert.AreEqual(System.TimeSpan.FromDays(1), e.Evaluate());
        }

        [TestMethod]
        public void DateTime_AddTimeSpan_ProducesNewDateTime()
        {
            var context = NewDateContext();
            var e = context.CompileDynamic("#15/12/2008# + ##1.00:00#");

            Assert.AreEqual(new System.DateTime(2008, 12, 16), e.Evaluate());
        }
    }
}
