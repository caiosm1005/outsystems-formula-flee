using System;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    [TestClass]
    public class ExpressionBuildingTest
    {
        [TestMethod]
        public void ExpressionsAsVariables()
        {
            ExpressionContext context = new();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 3.14);
            IDynamicExpression e1 = context.CompileDynamic("cos(a) * cos(a)");

            context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 3.14);

            IDynamicExpression e2 = context.CompileDynamic("sin(a) * sin(a)");

            // Use the two expressions as variables in another expression
            context = new ExpressionContext();
            context.Variables.Add("a", e1);
            context.Variables.Add("b", e2);
            IDynamicExpression e = context.CompileDynamic("a + b");

            Console.WriteLine(e.Evaluate());
        }


        [TestMethod]
        public void Test_IfExpression_enUS()
        {
            ExpressionContext context = new();
            context.Options.ParseCulture = new System.Globalization.CultureInfo("en-US");

            int resultWhenTrue = 3;

            IDynamicExpression e = context.CompileDynamic("if(1<2, 3, 4)");

            Assert.IsTrue((int)e.Evaluate() == resultWhenTrue);
        }

        [TestMethod]
        public void Test_IfExpression_fiFI()
        {
            ExpressionContext context = new();
            context.Imports.AddType(typeof(Math));
            context.Options.ParseCulture = new System.Globalization.CultureInfo("fi-FI");

            int resultWhenFalse = 4;

            IDynamicExpression e = context.CompileDynamic("if(1>2; 3; 4)");

            Assert.IsTrue((int)e.Evaluate() == resultWhenFalse);
        }

        [TestMethod]
        public void CompareLongs()
        {
            // bug #83 test.
            ExpressionContext context = new();
            IDynamicExpression e1 = context.CompileDynamic(
                "2432696330L = 2432696330L AND 2432696330L > 0 AND 2432696330L < 2432696331L");

            Assert.IsTrue((bool)e1.Evaluate());
            e1 = context.CompileDynamic("2432696330L / 2");

            Assert.AreEqual(1216348165L, e1.Evaluate());
        }

        [TestMethod]
        public void ArgumentInt_to_DoubleConversion()
        {
            ExpressionContext context = new();
            context.Imports.AddType(typeof(Math));
            IDynamicExpression e1 = context.CompileDynamic("sqrt(16)");

            Assert.AreEqual(4.0, e1.Evaluate());
        }


        [TestMethod]
        public void IN_OperatorTest()
        {
            ExpressionContext context = new();
            var e1 = context.CompileGeneric<bool>(
                "NOT 15 IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14,16,17,18,19,20,21,22,23)");

            Assert.IsTrue(e1.Evaluate());

            e1 = context.CompileGeneric<bool>("\"a\" IN (\"a\",\"b\",\"c\",\"d\") and true and 5 in (2,4,5)");
            Assert.IsTrue(e1.Evaluate());
            e1 = context.CompileGeneric<bool>("\"a\" IN (\"a\",\"b\",\"c\",\"d\") and true and 5 in (2,4,6,7,8,9)");
            Assert.IsFalse(e1.Evaluate());
        }
    }
}