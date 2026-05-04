using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.ExpressionTests
{
    /// <summary>
    /// Exercises the public API on <see cref="IExpression"/>: dynamic vs generic compilation,
    /// <see cref="IExpression.Clone"/>, the <see cref="IExpression.Text"/> property, and
    /// <see cref="ExpressionInfo"/> metadata.
    /// </summary>
    [TestClass]
    public class CompilationApiTests
    {
        // --- Dynamic vs generic ---

        [TestMethod]
        public void CompileDynamic_ReturnsObjectBoxedResult()
        {
            var context = new ExpressionContext();
            IDynamicExpression e = context.CompileDynamic("1 + 2");

            object result = e.Evaluate();
            Assert.AreEqual(3, result);
            Assert.AreEqual(typeof(int), result.GetType());
        }

        [TestMethod]
        public void CompileGeneric_StronglyTypedResult()
        {
            var context = new ExpressionContext();
            IGenericExpression<int> e = context.CompileGeneric<int>("1 + 2");

            int result = e.Evaluate();
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void CompileGeneric_PromotesIntResultToDouble()
        {
            var context = new ExpressionContext();
            // Body is integer arithmetic, but the requested result type is double.
            IGenericExpression<double> e = context.CompileGeneric<double>("1 + 2");

            Assert.AreEqual(3.0, e.Evaluate());
        }

        [TestMethod]
        public void CompileGeneric_BoolResult()
        {
            var context = new ExpressionContext();
            IGenericExpression<bool> e = context.CompileGeneric<bool>("1 < 2 AND 3 > 2");

            Assert.IsTrue(e.Evaluate());
        }

        [TestMethod]
        public void CompileGeneric_StringResult()
        {
            var context = new ExpressionContext();
            IGenericExpression<string> e = context.CompileGeneric<string>("\"hello \" + \"world\"");

            Assert.AreEqual("hello world", e.Evaluate());
        }

        // --- Text property ---

        [TestMethod]
        public void Text_EchoesOriginalSource()
        {
            var context = new ExpressionContext();
            const string source = "1 + 2 * 3";
            var e = context.CompileDynamic(source);

            Assert.AreEqual(source, e.Text);
        }

        // --- Clone ---

        [TestMethod]
        public void Clone_ProducesIndependentExpression()
        {
            var context = new ExpressionContext();
            context.Variables.Add("x", 5);
            var e = context.CompileDynamic("x * 2");

            var clone = (IDynamicExpression)e.Clone();
            // Mutating the clone's variables doesn't affect the original.
            clone.Context.Variables["x"] = 10;

            Assert.AreEqual(10, e.Evaluate());
            Assert.AreEqual(20, clone.Evaluate());
        }

        [TestMethod]
        public void Clone_PreservesText()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("42 + 1");
            var clone = e.Clone();

            Assert.AreEqual(e.Text, clone.Text);
        }

        // --- ExpressionInfo ---

        [TestMethod]
        public void Info_NoVariablesReferenced_ReturnsEmptyArray()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 2 * 3");

            Assert.AreEqual(0, e.Info.GetReferencedVariables().Length);
        }

        [TestMethod]
        public void Info_SingleVariableReferenced_ReturnsThatName()
        {
            var context = new ExpressionContext();
            context.Variables.Add("myVar", 10);
            var e = context.CompileDynamic("myVar + 1");

            string[] names = e.Info.GetReferencedVariables();
            Assert.AreEqual(1, names.Length);
            Assert.AreEqual("myVar", names[0]);
        }

        // --- Evaluating after the source variable changes ---

        [TestMethod]
        public void Evaluate_ReflectsLatestVariableValue()
        {
            var context = new ExpressionContext();
            context.Variables.Add("x", 1);
            var e = context.CompileDynamic("x * 10");

            Assert.AreEqual(10, e.Evaluate());

            // Compiled expressions clone the context, so updates must go through the
            // expression's own context.
            e.Context.Variables["x"] = 7;
            Assert.AreEqual(70, e.Evaluate());
        }

        // --- Context is exposed ---

        [TestMethod]
        public void Context_IsAccessibleFromCompiledExpression()
        {
            var context = new ExpressionContext();
            var e = context.CompileDynamic("1 + 1");

            Assert.IsNotNull(e.Context);
            Assert.IsNotNull(e.Context.Options);
        }
    }
}
