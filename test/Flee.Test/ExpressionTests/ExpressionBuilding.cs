using System;
using System.Globalization;
using Flee.PublicTypes;
using NUnit.Framework;

namespace Flee.Test.ExpressionTests
{
    [TestFixture]
    public class ExpressionBuilding
    {
        [Test]
        public void TestExpressionsAsVariables()
        {
            ExpressionContext context = new();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 3.14);
            IDynamicExpression e1 = context.CompileDynamic("cos(a) ^ 2");

            context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Variables.Add("a", 3.14);

            IDynamicExpression e2 = context.CompileDynamic("sin(a) ^ 2");

            // Use the two expressions as variables in another expression
            context = new ExpressionContext();
            context.Variables.Add("a", e1);
            context.Variables.Add("b", e2);
            IDynamicExpression e = context.CompileDynamic("a + b");

            Console.WriteLine(e.Evaluate());
        }


        [Test]
        public void TestIfExpression_enUS()
        {
            ExpressionContext context = new();
            context.Options.ParseCulture = new CultureInfo("en-US");

            int resultWhenTrue = 3;

            IDynamicExpression e = context.CompileDynamic("if(1<2, 3, 4)");

            Assert.IsTrue((int)e.Evaluate() == resultWhenTrue);
        }

        [Test]
        public void TestIfExpression_fiFI()
        {
            ExpressionContext context = new();
            context.Imports.AddType(typeof(Math));
            context.Options.ParseCulture = new CultureInfo("fi-FI");

            int resultWhenFalse = 4;

            IDynamicExpression e = context.CompileDynamic("if(1>2; 3; 4)");

            Assert.IsTrue((int)e.Evaluate() == resultWhenFalse);
        }

        [Test]
        public void TestNullCheck()
        {
            ExpressionContext context = new();
            context.Variables.Add("a", "stringObject");
            IDynamicExpression e1 = context.CompileDynamic("a = null");

            Assert.IsFalse((bool)e1.Evaluate());
        }

        [Test]
        public void TestNullIsNullCheck()
        {
            ExpressionContext context = new();
            context.Variables.Add("a", "stringObject");
            IDynamicExpression e1 = context.CompileDynamic("null = null");

            Assert.IsTrue((bool)e1.Evaluate());
        }

        [Test]
        public void TestCompareLongs()
        {
            // bug #83 test.
            ExpressionContext context = new();
            IDynamicExpression e1 = context.CompileDynamic("2432696330L = 2432696330L AND 2432696330L > 0 AND 2432696330L < 2432696331L");

            Assert.IsTrue((bool)e1.Evaluate());
            e1 = context.CompileDynamic("2432696330L / 2");

            Assert.AreEqual(1216348165L, e1.Evaluate());
        }

        [Test]
        public void TestArgumentIntToDoubleConversion()
        {
            ExpressionContext context = new();
            context.Imports.AddType(typeof(Math));
            IDynamicExpression e1 = context.CompileDynamic("sqrt(16)");

            Assert.AreEqual(4.0, e1.Evaluate());
        }

        [Test]
        public void TestInOperator()
        {
            ExpressionContext context = new();
            context.Options.ParseCulture = new CultureInfo("en-US"); // Set default culture
            IGenericExpression<bool> e1 = context.CompileGeneric<bool>("NOT 15 IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14,16,17,18,19,20,21,22,23)");

            Assert.IsTrue(e1.Evaluate());

            e1 = context.CompileGeneric<bool>("\"a\" IN (\"a\",\"b\",\"c\",\"d\") and true and 5 in (2,4,5)");
            Assert.IsTrue(e1.Evaluate());
            e1 = context.CompileGeneric<bool>("\"a\" IN (\"a\",\"b\",\"c\",\"d\") and true and 5 in (2,4,6,7,8,9)");
            Assert.IsFalse(e1.Evaluate());
        }

        [Test]
        public void TestDateTimeFormat()
        {
            ExpressionContext context = new();
            context.ParserOptions.DateTimeFormats = new string[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };

            IGenericExpression<DateTime> e1 = context.CompileGeneric<DateTime>("#2025-05-10#");
            IGenericExpression<DateTime> e2 = context.CompileGeneric<DateTime>("#2025-05-10 12:12:12#");

            Assert.AreEqual(new DateTime(2025, 5, 10), e1.Evaluate());
            Assert.AreEqual(new DateTime(2025, 5, 10, 12, 12, 12), e2.Evaluate());
        }

        [Test]
        public void TestImplicitConversions()
        {
            HelperExpressionClass helper = new();
            ExpressionContext context = new(helper);
            context.Options.CaseSensitive = false;
            context.Options.ParseCulture = new CultureInfo("en-US"); // Set default culture
            context.ParserOptions.DateTimeFormats = new string[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };
            helper.Context = context;

            IGenericExpression<string> e1 = context.CompileGeneric<string>("#2025-05-10#");
            IGenericExpression<string> e2 = context.CompileGeneric<string>("#2025-05-10 00:00:00# + \"foobar\"");
            IGenericExpression<string> e3 = context.CompileGeneric<string>("#2025-05-10 12:12:12# + \"foobar\"");
            IGenericExpression<string> e4 = context.CompileGeneric<string>("MethodWithStringInput(#2025-05-10 12:12:12#)");
            IGenericExpression<string> e5 = context.CompileGeneric<string>("MethodWithStringInput(42.420)");
            IGenericExpression<string> e6 = context.CompileGeneric<string>("MethodWithStringInput(true)");
            IGenericExpression<bool> e7 = context.CompileGeneric<bool>("42.42 = \"42.420\"");
            IGenericExpression<bool> e8 = context.CompileGeneric<bool>("42.420 = \"42.42\"");
            IGenericExpression<bool> e9 = context.CompileGeneric<bool>("42.0 = 42");

            Assert.AreEqual("2025-05-10", e1.Evaluate());
            Assert.AreEqual("2025-05-10foobar", e2.Evaluate());
            Assert.AreEqual("2025-05-10 12:12:12foobar", e3.Evaluate());
            Assert.AreEqual("2025-05-10 12:12:12", e4.Evaluate());
            Assert.AreEqual("42.42", e5.Evaluate());
            Assert.AreEqual("True", e6.Evaluate());
            Assert.IsFalse(e7.Evaluate());
            Assert.IsTrue(e8.Evaluate());
            Assert.IsTrue(e9.Evaluate());
        }
    }

    public class HelperExpressionClass
    {
        public ExpressionContext Context;

        public string MethodWithStringInput(string input)
        {
            if (Context == null)
            {
                throw new InvalidOperationException("Context is not set.");
            }
            return input;
        }
    }
}