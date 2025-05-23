﻿using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using NUnit.Framework;
using System.Collections.Generic;

namespace Flee.Tests.CalcEngineTests
{
    [TestFixture]
    public class CalcEngineTestFixture
    {
        [Test]
        public void TestBasic()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y", context);
            ce.Add("c", "b * 2", context);
            ce.Recalculate("a");

            var result = ce.GetResult<int>("c");
            Assert.AreEqual(result, ((100 * 2) + 1) * 2);
            variables.Remove("x");
            variables.Add("x", 345);
            ce.Recalculate("a");
            result = ce.GetResult<int>("c");

            Assert.AreEqual(((345 * 2) + 1) * 2, result);
        }

        [Test]
        public void TestMutipleIdenticalReferences()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            ce.Add("b", "a + a + a", context);
            ce.Recalculate("a");
            var result = ce.GetResult<int>("b");
            Assert.AreEqual((100 * 2) * 3, result);
        }

        [Test]
        public void TestComplex()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 24);
            ce.Add("b", "y * 2", context);
            ce.Add("c", "a + b", context);
            ce.Add("d", "80", context);
            ce.Add("e", "a + b + c + d", context);
            ce.Recalculate("d");
            ce.Recalculate("a", "b");

            var result = ce.GetResult<int>("e");
            Assert.AreEqual((100 * 2) + (24 * 2) + ((100 * 2) + (24 * 2)) + 80, result);
        }

        [Test]
        public void TestArithmetic()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("a", 10);
            variables.Add("b", 20);
            ce.Add("x", "((a * 2) + (b ^ 2)) - (100 % 5)", context);
            ce.Recalculate("x");
            var result = ce.GetResult<int>("x");
            Assert.AreEqual(420, result);
        }

        [Test]
        public void TestComparisonOperators()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("a", 10);
            ce.Add("x", "a <> 100", context);
            ce.Recalculate("x");
            var result = ce.GetResult<bool>("x");
            Assert.IsTrue(result);
        }

        [Test]
        public void TestAndOrXorNotOperators()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("a", 10);
            ce.Add("x", "a > 100", context);
            ce.Recalculate("x");
            var result = ce.GetResult<bool>("x");
            Assert.IsFalse(result);
            ce.Remove("x");
            variables.Add("b", 100);
            ce.Add("x", "b = 100", context);
            ce.Recalculate("x");
            result = ce.GetResult<bool>("x");
            Assert.IsTrue(result);
        }

        [Test]
        public void TestShiftOperators()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            ce.Add("x", "100 >> 2", context);
            ce.Recalculate("x");
            var result = ce.GetResult<int>("x");
            Assert.AreEqual(25, result);
        }

        [Test]
        public void TestRecalculateNonSource()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y", context);
            ce.Add("c", "b * 2", context);
            ce.Recalculate("a", "b");
            var result = ce.GetResult<int>("c");
            Assert.AreEqual(((100) * 2 + 1) * 2, result);
        }

        [Test]
        public void TestPartialRecalculate()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y", context);
            ce.Add("c", "b * 2", context);
            ce.Recalculate("a");
            variables["y"] = 222;
            ce.Recalculate("b");
            var result = ce.GetResult<int>("c");
            Assert.AreEqual(((100 * 2) + 222) * 2, result);
        }

        [Test]
        public void TestCircularReference1()
        {
            var ce = new CalculationEngine();
            var context = new ExpressionContext();
            var variables = context.Variables;

            variables.Add("x", 100);
            ce.Add("a", "x * 2", context);
            variables.Add("y", 1);
            ce.Add("b", "a + y + b", context);
            Assert.Throws<CircularReferenceException>(() => { ce.Recalculate("a"); });
        }

        [Test]
        public void TestBooleanExpression()
        {
            string expression = "a AND NOT b AND NOT c AND d";
            Dictionary<string, object> expressionVariables = new Dictionary<string, object>();
            expressionVariables.Add("a", 1);
            expressionVariables.Add("b", 0);
            expressionVariables.Add("c", 0);
            expressionVariables.Add("d", 1);

            var context = new ExpressionContext();
            var vars = context.Variables;
            foreach (var expressionVariable in expressionVariables.Keys)
                vars.Add(expressionVariable, expressionVariables[expressionVariable]);
            IDynamicExpression dynamicExpression = context.CompileDynamic(expression);
            foreach (var expressionVariable in expressionVariables.Keys)
                vars[expressionVariable] = expressionVariables[expressionVariable];
            var a = dynamicExpression.Evaluate();
        }
    }
}