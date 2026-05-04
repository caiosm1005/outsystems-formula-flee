using System;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the <see cref="VariableCollection"/> events that let callers resolve unknown
    /// variables and functions on demand.
    /// </summary>
    [TestClass]
    public class VariableResolutionTests
    {
        // --- Variable add / get / remove ---

        [TestMethod]
        public void Variable_AddAndRead_RoundTrips()
        {
            var context = new ExpressionContext();
            context.Variables.Add("x", 10);
            var e = context.CompileDynamic("x * 2");

            Assert.AreEqual(20, e.Evaluate());
        }

        [TestMethod]
        public void Variable_DefineThenSet_AcceptsLaterValues()
        {
            var context = new ExpressionContext();
            context.Variables.DefineVariable("x", typeof(int));
            var e = context.CompileDynamic("x * 2");

            context.Variables["x"] = 5;
            Assert.AreEqual(10, e.Evaluate());

            context.Variables["x"] = 7;
            Assert.AreEqual(14, e.Evaluate());
        }

        [TestMethod]
        public void Variable_AddTwiceWithSameName_Throws()
        {
            var context = new ExpressionContext();
            context.Variables.Add("x", 10);

            Assert.ThrowsException<ArgumentException>(() => context.Variables.Add("x", 20));
        }

        [TestMethod]
        public void Variable_GetUnknownByIndexer_Throws()
        {
            var context = new ExpressionContext();

            Assert.ThrowsException<ArgumentException>(() => _ = context.Variables["nope"]);
        }

        [TestMethod]
        public void Variable_TryGetValueMissing_ReturnsFalse()
        {
            var context = new ExpressionContext();

            bool found = context.Variables.TryGetValue("nope", out object? value);
            Assert.IsFalse(found);
            Assert.IsNull(value);
        }

        [TestMethod]
        public void Variable_AddWithNullValue_Throws()
        {
            var context = new ExpressionContext();

            Assert.ThrowsException<ArgumentNullException>(() => context.Variables.Add("x", null!));
        }

        [TestMethod]
        public void Variable_GetVariableType_ReturnsRegisteredType()
        {
            var context = new ExpressionContext();
            context.Variables.Add("x", "hello");

            Assert.AreEqual(typeof(string), context.Variables.GetVariableType("x"));
        }

        // --- ResolveVariableType / ResolveVariableValue ---

        [TestMethod]
        public void ResolveVariableType_DeclaresType_AndValueResolverProvidesValue()
        {
            var context = new ExpressionContext();

            context.Variables.ResolveVariableType += (_, args) =>
            {
                if (args.VariableName == "ondemand")
                {
                    args.VariableType = typeof(int);
                }
            };

            context.Variables.ResolveVariableValue += (_, args) =>
            {
                if (args.VariableName == "ondemand")
                {
                    args.VariableValue = 42;
                }
            };

            var e = context.CompileDynamic("ondemand + 8");
            Assert.AreEqual(50, e.Evaluate());
        }

        [TestMethod]
        public void ResolveVariableType_NoMatch_ProducesCompileError()
        {
            var context = new ExpressionContext();
            context.Variables.ResolveVariableType += (_, args) => { /* no-op */ };

            Assert.ThrowsException<ExpressionCompileException>(() => context.CompileDynamic("nope"));
        }

        [TestMethod]
        public void ResolveVariableValue_CalledFreshlyEachEvaluation()
        {
            var context = new ExpressionContext();
            int callCount = 0;

            context.Variables.ResolveVariableType += (_, args) =>
            {
                if (args.VariableName == "counter")
                {
                    args.VariableType = typeof(int);
                }
            };

            context.Variables.ResolveVariableValue += (_, args) =>
            {
                if (args.VariableName == "counter")
                {
                    callCount++;
                    args.VariableValue = callCount;
                }
            };

            var e = context.CompileDynamic("counter");
            Assert.AreEqual(1, e.Evaluate());
            Assert.AreEqual(2, e.Evaluate());
            Assert.AreEqual(3, e.Evaluate());
        }

        // --- ResolveFunction / InvokeFunction ---

        [TestMethod]
        public void ResolveFunction_DeclaresReturnType_AndInvokeProvidesResult()
        {
            var context = new ExpressionContext();

            context.Variables.ResolveFunction += (_, args) =>
            {
                if (args.FunctionName == "Doubled")
                {
                    args.ReturnType = typeof(int);
                }
            };

            context.Variables.InvokeFunction += (_, args) =>
            {
                if (args.FunctionName == "Doubled")
                {
                    args.Result = (int)args.Arguments[0] * 2;
                }
            };

            var e = context.CompileDynamic("Doubled(21)");
            Assert.AreEqual(42, e.Evaluate());
        }

        [TestMethod]
        public void InvokeFunction_ReceivesEvaluatedArgumentsInOrder()
        {
            var context = new ExpressionContext();
            object[]? received = null;

            context.Variables.ResolveFunction += (_, args) =>
            {
                if (args.FunctionName == "First")
                {
                    args.ReturnType = typeof(int);
                }
            };

            context.Variables.InvokeFunction += (_, args) =>
            {
                received = args.Arguments;
                args.Result = (int)args.Arguments[0];
            };

            var e = context.CompileDynamic("First(1+1, 2*5, 100)");
            Assert.AreEqual(2, e.Evaluate());
            Assert.IsNotNull(received);
            Assert.AreEqual(3, received!.Length);
            Assert.AreEqual(2, received[0]);
            Assert.AreEqual(10, received[1]);
            Assert.AreEqual(100, received[2]);
        }

        // --- Expressions stored as variables ---

        [TestMethod]
        public void Variable_HoldsCompiledExpression_AndIsUsedTransparently()
        {
            var inner = new ExpressionContext();
            var innerExpr = inner.CompileDynamic("3 * 4");

            var outer = new ExpressionContext();
            outer.Variables.Add("inner", innerExpr);
            var e = outer.CompileDynamic("inner + 1");

            Assert.AreEqual(13, e.Evaluate());
        }

        // --- ExpressionInfo records referenced variables ---

        [TestMethod]
        public void ExpressionInfo_GetReferencedVariables_ListsBothNames()
        {
            var context = new ExpressionContext();
            context.Variables.Add("a", 1);
            context.Variables.Add("b", 2);
            var e = context.CompileDynamic("a + b + a");

            string[] names = e.Info.GetReferencedVariables();
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, names);
        }
    }
}
