using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises AND/OR/NOT in both their bitwise (integer) and logical (boolean) forms.
    /// </summary>
    [TestClass]
    public class LogicalAndBitwiseTests
    {
        // --- Bitwise on integers ---

        [TestMethod]
        public void And_TwoInts_ReturnsBitwiseAnd()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("12345 AND 12");

            Assert.AreEqual(12345 & 12, e.Evaluate());
        }

        [TestMethod]
        public void Or_TwoInts_ReturnsBitwiseOr()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("800 OR 12");

            Assert.AreEqual(800 | 12, e.Evaluate());
        }

        [TestMethod]
        public void Not_Int_ReturnsBitwiseComplement()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT 0");

            Assert.AreEqual(-1, e.Evaluate());
        }

        [TestMethod]
        public void Not_Int_ComplementOfNegativeOneIsZero()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT -1");

            Assert.AreEqual(0, e.Evaluate());
        }

        [TestMethod]
        public void Bitwise_ChainedAnd_IsLeftAssociative()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("123 AND 100 AND 1245 AND 80");

            Assert.AreEqual(123 & 100 & 1245 & 80, e.Evaluate());
        }

        [TestMethod]
        public void Bitwise_LongAnd_StaysAsLong()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("100L AND 200L");

            Assert.AreEqual(100L & 200L, e.Evaluate());
        }

        // --- Logical on booleans ---

        [TestMethod]
        public void And_TwoBools_ReturnsLogicalAnd()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("true AND false");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Or_TwoBools_ReturnsLogicalOr()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("true OR false");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Not_Bool_NegatesValue()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT false");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Logical_AndHigherPrecedenceThanOr()
        {
            // false OR (true AND true) = true ; not (false OR true) AND true = true coincidentally,
            // so use a case where the difference matters.
            // (false AND true) OR true = true ; false AND (true OR true) = false
            var context = new ExpressionContext();
            var e = context.CompileDynamic("false AND true OR true");

            Assert.AreEqual(true, e.Evaluate());
        }

        [TestMethod]
        public void Logical_NotBindsTighterThanAnd()
        {
            // (NOT false) AND true should be true; NOT (false AND true) is also true,
            // but (NOT true) AND true is false versus NOT (true AND true) = false; use precedence-sensitive case.
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT false AND false");

            Assert.AreEqual(false, e.Evaluate());
        }

        [TestMethod]
        public void Logical_ParensOverridePrecedence()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT (true AND true)");

            Assert.AreEqual(false, e.Evaluate());
        }

        // --- Short-circuit evaluation ---

        public sealed class ShortCircuitOwner
        {
            public int CallCount { get; set; }

            public bool BoomTrue()
            {
                CallCount++;
                return true;
            }

            public bool BoomFalse()
            {
                CallCount++;
                return false;
            }
        }

        [TestMethod]
        public void Or_LeftIsTrue_DoesNotEvaluateRight()
        {
            var owner = new ShortCircuitOwner();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("true OR BoomTrue()");

            Assert.AreEqual(true, e.Evaluate());
            Assert.AreEqual(0, owner.CallCount);
        }

        [TestMethod]
        public void And_LeftIsFalse_DoesNotEvaluateRight()
        {
            var owner = new ShortCircuitOwner();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("false AND BoomTrue()");

            Assert.AreEqual(false, e.Evaluate());
            Assert.AreEqual(0, owner.CallCount);
        }

        [TestMethod]
        public void Or_LeftIsFalse_EvaluatesRight()
        {
            var owner = new ShortCircuitOwner();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("false OR BoomTrue()");

            Assert.AreEqual(true, e.Evaluate());
            Assert.AreEqual(1, owner.CallCount);
        }

        [TestMethod]
        public void And_LeftIsTrue_EvaluatesRight()
        {
            var owner = new ShortCircuitOwner();
            var context = new ExpressionContext(owner);
            var e = context.CompileDynamic("true AND BoomFalse()");

            Assert.AreEqual(false, e.Evaluate());
            Assert.AreEqual(1, owner.CallCount);
        }

        // --- Bitwise NOT preserves type ---

        [TestMethod]
        public void Not_Long_ReturnsLongComplement()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("NOT 0L");

            Assert.AreEqual(-1L, e.Evaluate());
        }

        // --- Combined ---

        [TestMethod]
        public void Combined_AndOr_AreLeftAssociative()
        {
            // Unlike C#, Flee gives AND/OR the same precedence and chains them left-to-right:
            // 123 AND 100 OR 1245 → (123 AND 100) OR 1245
            var context = new ExpressionContext();
            var e = context.CompileDynamic("123 AND 100 OR 1245");

            int expected = (123 & 100) | 1245;
            Assert.AreEqual(expected, e.Evaluate());
        }
    }
}
