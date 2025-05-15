using System;
using System.Globalization;
using Flee.PublicTypes;
using NUnit.Framework;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1861 // Avoid constant arrays as arguments

namespace Flee.Test.ExpressionTests
{
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

        public string[] StringSplit(string input, string separator)
        {
            return input.Split(new[] { separator }, StringSplitOptions.None);
        }
    }

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
            HelperExpressionClass helper = new();
            ExpressionContext context = new(helper);
            context.Imports.AddType(typeof(HelperExpressionClass));
            context.Options.ParseCulture = new CultureInfo("en-US"); // Set default culture
            context.Variables.Add("a", new[] { "string1", "string2" });

            IGenericExpression<bool> e1 = context.CompileGeneric<bool>("NOT 15 IN (1,2,3,4,5,6,7,8,9,10,11,12,13,14,16,17,18,19,20,21,22,23)");
            IGenericExpression<bool> e2 = context.CompileGeneric<bool>("\"a\" IN (\"a\",\"b\",\"c\",\"d\") and true and 5 in (2,4,5)");
            IGenericExpression<bool> e3 = context.CompileGeneric<bool>("\"a\" IN (\"a\",\"b\",\"c\",\"d\") and true and 5 in (2,4,6,7,8,9)");
            IGenericExpression<bool> e4 = context.CompileGeneric<bool>("\"string1\" IN a");
            IGenericExpression<bool> e5 = context.CompileGeneric<bool>("\"string2\" IN StringSplit(\"string1,string2\", \",\") and not \"invalid\" in StringSplit(\"x,y,z\", \",\")");

            Assert.IsTrue(e1.Evaluate());
            Assert.IsTrue(e2.Evaluate());
            Assert.IsFalse(e3.Evaluate());
            Assert.IsTrue(e4.Evaluate());
            Assert.IsTrue(e5.Evaluate());
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

            IGenericExpression<string> dateTimeE1 = context.CompileGeneric<string>("#2025-05-10#");
            IGenericExpression<string> dateTimeE2 = context.CompileGeneric<string>("#2025-05-10 00:00:00# + \"foobar\"");
            IGenericExpression<string> dateTimeE3 = context.CompileGeneric<string>("#2025-05-10 12:12:12# + \"foobar\"");
            IGenericExpression<string> dateTimeE4 = context.CompileGeneric<string>("#2025-05-10#.AddYears(1.9999999999999999)");
            
            IGenericExpression<string> methodE1 = context.CompileGeneric<string>("MethodWithStringInput(#2025-05-10 12:12:12#)");
            IGenericExpression<string> methodE2 = context.CompileGeneric<string>("MethodWithStringInput(42.420)");
            IGenericExpression<string> methodE3 = context.CompileGeneric<string>("MethodWithStringInput(true)");

            IGenericExpression<string> doubleE1 = context.CompileGeneric<string>("42.420");
            IGenericExpression<string> doubleE2 = context.CompileGeneric<string>("42.000");
            IGenericExpression<bool> doubleE3 = context.CompileGeneric<bool>("42.42 = \"42.420\"");
            IGenericExpression<bool> doubleE4 = context.CompileGeneric<bool>("42.420 = \"42.42\"");
            IGenericExpression<bool> doubleE5 = context.CompileGeneric<bool>("42.0 = 42");
            IGenericExpression<double> doubleE6 = context.CompileGeneric<double>("42");
            IGenericExpression<int> doubleE7 = context.CompileGeneric<int>("42.42");
            IGenericExpression<uint> doubleE8 = context.CompileGeneric<uint>("42.42");
            IGenericExpression<long> doubleE9 = context.CompileGeneric<long>("41.9999999999999999");

            Assert.AreEqual("2025-05-10", dateTimeE1.Evaluate());
            Assert.AreEqual("2025-05-10foobar", dateTimeE2.Evaluate());
            Assert.AreEqual("2025-05-10 12:12:12foobar", dateTimeE3.Evaluate());
            Assert.AreEqual("2027-05-10", dateTimeE4.Evaluate());
            
            Assert.AreEqual("2025-05-10 12:12:12", methodE1.Evaluate());
            Assert.AreEqual("42.42", methodE2.Evaluate());
            Assert.AreEqual("True", methodE3.Evaluate());
            
            Assert.AreEqual("42.42", doubleE1.Evaluate());
            Assert.AreEqual("42", doubleE2.Evaluate());
            Assert.IsFalse(doubleE3.Evaluate());
            Assert.IsTrue(doubleE4.Evaluate());
            Assert.IsTrue(doubleE5.Evaluate());
            Assert.AreEqual(42.0, doubleE6.Evaluate());
            Assert.AreEqual(42, doubleE7.Evaluate());
            Assert.AreEqual(42, doubleE8.Evaluate());
            Assert.AreEqual(42, doubleE9.Evaluate());
        }

        [Test]
        public void TestInstanceMembersAccess()
        {
            ExpressionContext c1 = new();
            ExpressionContext c2 = new();
            c2.ParserOptions.AllowMemberAccess = false;

            IGenericExpression<string> e1 = c1.CompileGeneric<string>("\"Hello World\".ToUpper()");
            
            Assert.AreEqual("HELLO WORLD", e1.Evaluate());
            Assert.Catch<ExpressionCompileException>(() => c2.CompileGeneric<string>("\"Hello World\".ToUpper()"));
        }
    }
}