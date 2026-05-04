using System;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    [TestClass]
    public class ArithmeticOperatorTests
    {
        // --- Addition ---

        [TestMethod]
        public void Add_TwoInts_ReturnsInt()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("12 + 30");

            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void Add_NegativeAndPositive_ReturnsDifference()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-100 + 25");

            Assert.AreEqual(-75, e.Evaluate());
        }

        [TestMethod]
        public void Add_IntAndDouble_PromotesToDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 0.5");

            Assert.AreEqual(1.5, e.Evaluate());
        }

        [TestMethod]
        public void Add_DoubleAndInt_PromotesToDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("0.5 + 1");

            Assert.AreEqual(1.5, e.Evaluate());
        }

        [TestMethod]
        public void Add_IntAndLong_PromotesToLong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 2L");

            Assert.AreEqual(3L, e.Evaluate());
        }

        [TestMethod]
        public void Add_TwoStrings_Concatenates()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"foo\" + \"bar\"");

            Assert.AreEqual("foobar", e.Evaluate());
        }

        [TestMethod]
        public void Add_StringAndInt_ConcatenatesAsString()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("\"value=\" + 100");

            Assert.AreEqual("value=100", e.Evaluate());
        }

        [TestMethod]
        public void Add_IntAndString_ConcatenatesAsString()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 + \"!\"");

            Assert.AreEqual("100!", e.Evaluate());
        }

        // --- Subtraction ---

        [TestMethod]
        public void Subtract_TwoInts_ReturnsDifference()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100 - 42");

            Assert.AreEqual(58, e.Evaluate());
        }

        [TestMethod]
        public void Subtract_DoubleMinusDouble_ReturnsDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("3.5 - 1.25");

            Assert.AreEqual(2.25, e.Evaluate());
        }

        [TestMethod]
        public void Subtract_NegativeFromNegative_ReturnsExpected()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-10 - -5");

            Assert.AreEqual(-5, e.Evaluate());
        }

        // --- Multiplication ---

        [TestMethod]
        public void Multiply_TwoInts_ReturnsProduct()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("6 * 7");

            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void Multiply_NegativeAndPositive_ReturnsNegative()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-3 * 4");

            Assert.AreEqual(-12, e.Evaluate());
        }

        [TestMethod]
        public void Multiply_DoubleAndInt_PromotesToDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("2.5 * 4");

            Assert.AreEqual(10.0, e.Evaluate());
        }

        // --- Division ---

        [TestMethod]
        public void Divide_TwoInts_TruncatesTowardZero()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("7 / 2");

            Assert.AreEqual(3, e.Evaluate());
        }

        [TestMethod]
        public void Divide_NegativeIntByInt_TruncatesTowardZero()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-7 / 2");

            Assert.AreEqual(-3, e.Evaluate());
        }

        [TestMethod]
        public void Divide_TwoDoubles_KeepsFractionalPart()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("7.0 / 2.0");

            Assert.AreEqual(3.5, e.Evaluate());
        }

        [TestMethod]
        public void Divide_IntByZero_ThrowsAtEvaluation()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 / 0");

            Assert.ThrowsException<DivideByZeroException>(() => e.Evaluate());
        }

        [TestMethod]
        public void Divide_DoubleByZero_ReturnsInfinity()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.0 / 0.0");

            Assert.AreEqual(double.PositiveInfinity, e.Evaluate());
        }

        // --- Unary negate ---

        [TestMethod]
        public void Negate_PositiveInt_ReturnsNegative()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-(42)");

            Assert.AreEqual(-42, e.Evaluate());
        }

        [TestMethod]
        public void Negate_NegativeInt_ReturnsPositive()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-(-42)");

            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void Negate_DoubleLiteral_ReturnsNegativeDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-1.5");

            Assert.AreEqual(-1.5, e.Evaluate());
        }

        // --- Operator precedence and parentheses ---

        [TestMethod]
        public void Precedence_MultiplicationBeforeAddition()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 2 * 3");

            Assert.AreEqual(7, e.Evaluate());
        }

        [TestMethod]
        public void Precedence_ParensOverrideMultiplication()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("(1 + 2) * 3");

            Assert.AreEqual(9, e.Evaluate());
        }

        [TestMethod]
        public void Precedence_ChainedSubtractionIsLeftAssociative()
        {
            // (10 - 3) - 2 = 5; not 10 - (3 - 2) = 9
            var context = new ExpressionContext();
            var e = context.CompileDynamic("10 - 3 - 2");

            Assert.AreEqual(5, e.Evaluate());
        }

        // --- Decimal arithmetic via overloaded operators ---

        [TestMethod]
        public void Decimal_AddTwoDecimals_UsesDecimalOperator()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100.50M + 0.25M");

            Assert.AreEqual(100.75M, e.Evaluate());
        }

        [TestMethod]
        public void Decimal_MultiplyDecimalAndInt_PromotesToDecimal()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("2.5M * 4");

            Assert.AreEqual(10M, e.Evaluate());
        }

        [TestMethod]
        public void Decimal_NegateDecimal_ReturnsNegative()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("-100.45M");

            Assert.AreEqual(-100.45M, e.Evaluate());
        }

        // --- Long / unsigned arithmetic ---

        [TestMethod]
        public void Long_AddTwoLongs_StaysAsLong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("5000000000L + 5000000000L");

            Assert.AreEqual(10000000000L, e.Evaluate());
        }

        [TestMethod]
        public void Uint_AddTwoUints_StaysAsUint()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100U + 200U");

            Assert.AreEqual(300U, e.Evaluate());
        }

        [TestMethod]
        public void Uint_DivideTwoUints_StaysAsUint()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1024U / 4U");

            Assert.AreEqual(256U, e.Evaluate());
        }

        [TestMethod]
        public void Ulong_AddTwoUlongs_StaysAsUlong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100UL + 200UL");

            Assert.AreEqual(300UL, e.Evaluate());
        }

        // --- Single arithmetic ---

        [TestMethod]
        public void Single_AddTwoSingles_StaysAsSingle()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1.5f + 2.25f");

            Assert.AreEqual(3.75f, e.Evaluate());
        }

        [TestMethod]
        public void Single_SubtractAndPromoteToDouble()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("5.5 - 1.5f");

            Assert.AreEqual(4.0, e.Evaluate());
        }
    }
}
