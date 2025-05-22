using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Tests.ExpressionTests
{
    public class Base
    {
        public double Value { get; set; }
        public static Base operator +(Base left, Base right)
        {
            return new Base { Value = left.Value + right.Value };
        }
        public static Base operator -(Base left)
        {
            return new Base { Value = -left.Value };
        }
    }

    public class Derived : Base
    {
    }

    public class OtherDerived : Base
    {
    }

    [TestFixture]
    public class CustomOperators
    {
        [Test]
        public void TestLeftBaseRightBase()
        {
            var m1 = new Base { Value = 2 };
            var m2 = new Base { Value = 5 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            context.Variables.Add("m2", m2);
            IDynamicExpression e1 = context.CompileDynamic("m1 + m2");

            Base added = (Base) e1.Evaluate();
            Assert.AreEqual(7, added.Value);
        }

        [Test]
        public void TestLeftBaseRightDerived()
        {
            var m1 = new Base { Value = 2 };
            var m2 = new Derived { Value = 5 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            context.Variables.Add("m2", m2);
            IDynamicExpression e1 = context.CompileDynamic("m1 + m2");

            Base added = (Base)e1.Evaluate();
            Assert.AreEqual(7, added.Value);
        }

        [Test]
        public void TestLeftDerivedRightBase()
        {
            var m1 = new Derived { Value = 2 };
            var m2 = new Base { Value = 5 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            context.Variables.Add("m2", m2);
            IDynamicExpression e1 = context.CompileDynamic("m1 + m2");

            Base added = (Base)e1.Evaluate();
            Assert.AreEqual(7, added.Value);
        }

        [Test]
        public void TestLeftDerivedRightDerived()
        {
            var m1 = new Derived { Value = 2 };
            var m2 = new Derived { Value = 5 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            context.Variables.Add("m2", m2);
            IDynamicExpression e1 = context.CompileDynamic("m1 + m2");

            Base added = (Base)e1.Evaluate();
            Assert.AreEqual(7, added.Value);
        }

        [Test]
        public void TestLeftDerivedRightOtherDerived()
        {
            var m1 = new Derived { Value = 2 };
            var m2 = new OtherDerived { Value = 5 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            context.Variables.Add("m2", m2);
            IDynamicExpression e1 = context.CompileDynamic("m1 + m2");

            Base added = (Base)e1.Evaluate();
            Assert.AreEqual(7, added.Value);
        }

        [Test]
        public void TestMissingOperator()
        {
            var m1 = new Derived { Value = 2 };
            var m2 = new OtherDerived { Value = 5 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            context.Variables.Add("m2", m2);

            var message = "ArithmeticElement: Operation 'Subtract' is not defined for types 'Derived' and 'OtherDerived'";
            Assert.Throws<ExpressionCompileException>(() => context.CompileDynamic("m1 - m2"), message);
        }

        [Test]
        public void TestBaseUnaryOperator()
        {
            var m1 = new Base { Value = 2 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            IDynamicExpression e1 = context.CompileDynamic("-m1");

            Base negated = (Base)e1.Evaluate();
            Assert.AreEqual(-2, negated.Value);
        }

        [Test]
        public void TestDerivedUnaryOperator()
        {
            var m1 = new Derived { Value = 2 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            IDynamicExpression e1 = context.CompileDynamic("-m1");

            Base negated = (Base)e1.Evaluate();
            Assert.AreEqual(-2, negated.Value);
        }

        [Test]
        public void TestDerivedUnaryOperatorPlusOperator()
        {
            var m1 = new Derived { Value = 2 };

            ExpressionContext context = new ExpressionContext();
            context.Variables.Add("m1", m1);
            IDynamicExpression e1 = context.CompileDynamic("-m1 + m1");

            Base negated = (Base)e1.Evaluate();
            Assert.AreEqual(0, negated.Value);
        }
    }
}
