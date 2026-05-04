using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the &lt;&lt; and &gt;&gt; shift operators across signed and unsigned integers,
    /// including count truncation and the negative-count edge case.
    /// </summary>
    [TestClass]
    public class ShiftOperatorTests
    {
        // --- Basic shifts ---

        [TestMethod]
        public void ShiftLeft_TwoBits_MultipliesByFour()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 << 2");

            Assert.AreEqual(400, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_TwoBits_DividesByFour()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 >> 2");

            Assert.AreEqual(25, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_NegativeInt_PreservesSign()
        {
            // Arithmetic right shift on signed types: -200 >> 2 = -50.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-200 >> 2");

            Assert.AreEqual(-50, e.Evaluate());
        }

        [TestMethod]
        public void ShiftLeft_IntMaxValue_OverflowsToNegative()
        {
            var context = new ExpressionContext();
            context.Imports.ImportBuiltinTypes();
            var e = context.CompileDynamic("int.maxvalue << 2");

            Assert.AreEqual(-4, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_IntMinValue_StaysNegative()
        {
            // Arithmetic shift fills with 1s for negative.
            var context = new ExpressionContext();
            context.Imports.ImportBuiltinTypes();
            var e = context.CompileDynamic("int.minvalue >> 2");

            Assert.AreEqual(int.MinValue >> 2, e.Evaluate());
        }

        // --- Unsigned shifts ---

        [TestMethod]
        public void ShiftLeft_Uint_StaysUint()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100U << 2");

            Assert.AreEqual(400U, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_UintByOne_FillsWithZero()
        {
            // Logical right shift on unsigned types — top bit comes in as 0.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("0x40000000U >> 1");

            Assert.AreEqual(0x40000000U >> 1, e.Evaluate());
        }

        // --- Long shifts ---

        [TestMethod]
        public void ShiftLeft_Long_StaysLong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100L << 2");

            Assert.AreEqual(400L, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_LongMaxValue_PreservesSignBit()
        {
            var context = new ExpressionContext();
            context.Imports.ImportBuiltinTypes();
            var e = context.CompileDynamic("long.maxvalue >> 2");

            Assert.AreEqual(long.MaxValue >> 2, e.Evaluate());
        }

        // --- Shift count truncation ---

        [TestMethod]
        public void ShiftLeft_CountAbove31_IsTruncatedForInt()
        {
            // 100 << 100 → 100 << (100 & 31) = 100 << 4 = 1600
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 << 100");

            Assert.AreEqual(1600, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_CountEquals32_IsNoOpForInt()
        {
            // 1000 >> 32 → 1000 >> 0 = 1000 (count is masked with 0x1F)
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1000 >> 32");

            Assert.AreEqual(1000, e.Evaluate());
        }

        [TestMethod]
        public void ShiftRight_CountAbove63_IsTruncatedForLong()
        {
            // 1000L >> 64 → 1000L >> 0 = 1000L (count masked with 0x3F)
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1000L >> 64");

            Assert.AreEqual(1000L, e.Evaluate());
        }

        // --- Negative count behaves like the masked positive count ---

        [TestMethod]
        public void ShiftLeft_NegativeOne_BecomesShiftBy31()
        {
            // -1 cast to shift count is masked to 31, so 1 << -1 = 1 << 31 = int.MinValue
            var context = new ExpressionContext();
            var e = context.CompileDynamic("0x00000001 << -1");

            Assert.AreEqual(int.MinValue, e.Evaluate());
        }

        // --- Precedence with arithmetic ---

        [TestMethod]
        public void Shift_AdditionInCount_AppliedFirst()
        {
            // 800 >> (1+1) = 800 >> 2 = 200
            var context = new ExpressionContext();
            var e = context.CompileDynamic("800 >> 1+1");

            Assert.AreEqual(200, e.Evaluate());
        }

        [TestMethod]
        public void Shift_MultiplicationInValue_AppliedFirst()
        {
            // (100 * 2) << (1+1) = 200 << 2 = 800
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 * 2 << 1+1");

            Assert.AreEqual(800, e.Evaluate());
        }
    }
}
