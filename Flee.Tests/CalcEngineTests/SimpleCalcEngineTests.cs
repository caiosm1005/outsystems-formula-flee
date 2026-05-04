using System;
using Flee.CalcEngine.PublicTypes;
using Flee.PublicTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Flee.Tests.CalcEngineTests
{
    [TestClass]
    public class SimpleCalcEngineTests
    {
        [TestMethod]
        public void TestScripts()
        {
            var engine = new SimpleCalcEngine();
            var context = new ExpressionContext();
            context.Imports.AddType(typeof(Math));
            context.Imports.AddType(typeof(Math), "math");
            engine.Context = context;
        }
    }
}
