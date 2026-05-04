using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the explicit <c>cast()</c> operator and the implicit conversions inserted
    /// by the compiler.
    /// </summary>
    [TestClass]
    public class CastAndConversionTests
    {
        // --- Explicit cast: numeric narrowing / widening ---

        [TestMethod]
        public void Cast_DoubleToInt_TruncatesFraction()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(3.7, int)");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void Cast_NegativeDoubleToInt_TruncatesTowardZero()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(-3.7, int)");

            Assert.AreEqual(-3, e.Evaluate());
        }

        [TestMethod]
        public void Cast_IntToDouble_ConvertsExactly()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(100, double)");

            Assert.AreEqual(100.0, e.Evaluate());
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        [TestMethod]
        public void Cast_LongToInt_TruncatesUpperBits()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(100L, int)");

            Assert.AreEqual(100, e.Evaluate());
            Assert.AreEqual(typeof(int), e.Evaluate().GetType());
        }

        [TestMethod]
        public void Cast_IntToShort_FitsInRange()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(1000, short)");

            Assert.AreEqual((short)1000, e.Evaluate());
            Assert.AreEqual(typeof(short), e.Evaluate().GetType());
        }

        [TestMethod]
        public void Cast_IntToByte_FitsInRange()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(200, byte)");

            Assert.AreEqual((byte)200, e.Evaluate());
            Assert.AreEqual(typeof(byte), e.Evaluate().GetType());
        }

        // --- Explicit cast: char ↔ int ---

        [TestMethod]
        public void Cast_CharToInt_ReturnsCharCode()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast('A', int)");

            Assert.AreEqual(65, e.Evaluate());
        }

        [TestMethod]
        public void Cast_IntToChar_ReturnsCharFromCode()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(65, char)");

            Assert.AreEqual('A', e.Evaluate());
        }

        // --- Explicit cast: object boxing/unboxing ---

        [TestMethod]
        public void Cast_StringToObject_BoxesAsObject()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(\"abc\", object)");

            Assert.AreEqual("abc", e.Evaluate());
        }

        [TestMethod]
        public void Cast_NullToString_StillNull()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("cast(null, string)");

            Assert.IsNull(e.Evaluate());
        }

        // --- Implicit conversions in arithmetic ---

        [TestMethod]
        public void Implicit_IntPlusDouble_PromotesToDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 + 0.5");

            Assert.AreEqual(100.5, e.Evaluate());
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        [TestMethod]
        public void Implicit_IntPlusLong_PromotesToLong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 + 200L");

            Assert.AreEqual(300L, e.Evaluate());
            Assert.AreEqual(typeof(long), e.Evaluate().GetType());
        }

        [TestMethod]
        public void Implicit_IntPlusDecimal_PromotesToDecimal()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 + 0.5M");

            Assert.AreEqual(100.5M, e.Evaluate());
            Assert.AreEqual(typeof(decimal), e.Evaluate().GetType());
        }

        [TestMethod]
        public void Implicit_FloatPlusDouble_PromotesToDouble()
        {
            // 1.5f converts exactly to 1.5; 1.5f + 1.5 = 3.0
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5f + 1.5");

            Assert.AreEqual(3.0, e.Evaluate());
            Assert.AreEqual(typeof(double), e.Evaluate().GetType());
        }

        // --- Implicit conversion of a function argument ---

        [TestMethod]
        public void Implicit_IntArgToDoubleParam_AcceptsAndReturnsDouble()
        {
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(System.Math));
            var e = context.CompileDynamic("Sqrt(16)");

            Assert.AreEqual(4.0, e.Evaluate());
        }

        // --- Cast result type matches the destination ---

        [TestMethod]
        public void Cast_PreservesGenericResultType()
        {
            // CompileGeneric<int> with a cast that's compatible should round-trip cleanly.
            var context = new ExpressionContext();
            var e = context.CompileGeneric<int>("cast(3.5, int)");

            Assert.AreEqual(3, e.Evaluate());
        }
    }
}
