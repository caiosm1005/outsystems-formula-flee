﻿using System.ComponentModel.Design;
using System.Reflection.Emit;
using System.Reflection;
using Flee.ExpressionElements;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;
using Flee.Resources;
using IDynamicExpression = Flee.PublicTypes.IDynamicExpression;

namespace Flee.InternalTypes
{
    internal class Expression<T> : IExpression, IDynamicExpression, IGenericExpression<T>
    {
        private readonly string _myExpression;
        private ExpressionContext _myContext;
        private ExpressionOptions _myOptions;
        private readonly ExpressionInfo _myInfo;
        private ExpressionEvaluator<T> _myEvaluator;

        private object _myOwner;
        private const string EmitAssemblyName = "FleeExpression";

        private const string DynamicMethodName = "Flee Expression";
        public Expression(string expression, ExpressionContext context, bool isGeneric)
        {
            Utility.AssertNotNull(expression, nameof(expression));
            _myExpression = expression;
            _myOwner = context.ExpressionOwner;

            _myContext = context;

            if (context.NoClone == false)
            {
                _myContext = context.CloneInternal(false);
            }

            _myInfo = new ExpressionInfo();

            SetupOptions(_myContext.Options, isGeneric);

            _myContext.Imports.ImportOwner(_myOptions.OwnerType);

            ValidateOwner(_myOwner);

            Compile(expression, _myOptions);

            _myContext.CalculationEngine?.FixTemporaryHead(this, _myContext, _myOptions.ResultType);
        }

        private void SetupOptions(ExpressionOptions options, bool isGeneric)
        {
            // Make sure we clone the options
            _myOptions = options;
            _myOptions.IsGeneric = isGeneric;

            if (isGeneric)
            {
                _myOptions.ResultType = typeof(T);
            }

            _myOptions.SetOwnerType(_myOwner.GetType());
        }

        private void Compile(string expression, ExpressionOptions options)
        {
            // Add the services that will be used by elements during the compile
            IServiceContainer services = new ServiceContainer();
            AddServices(services);

            // Parse and get the root element of the parse tree
            ExpressionElement topElement = _myContext.Parse(expression, services);

            if (options.ResultType == null)
            {
                options.ResultType = topElement.ResultType;
            }

            RootExpressionElement rootElement = new(topElement, options.ResultType);

            DynamicMethod dm = CreateDynamicMethod();

            FleeILGenerator ilg = new(dm.GetILGenerator());

            // Emit the IL
            rootElement.Emit(ilg, services);
            if (ilg.NeedsSecondPass())
            {
                // second pass required due to long branches.
                dm = CreateDynamicMethod();
                ilg.PrepareSecondPass(dm.GetILGenerator());
                rootElement.Emit(ilg, services);
            }

            ilg.ValidateLength();

            // Emit to an assembly if required
            if (options.EmitToAssembly == true)
            {
                EmitToAssembly(ilg, rootElement, services);
            }

            Type delegateType = typeof(ExpressionEvaluator<>).MakeGenericType(typeof(T));
            _myEvaluator = (ExpressionEvaluator<T>)dm.CreateDelegate(delegateType);
        }

        private DynamicMethod CreateDynamicMethod()
        {
            // Create the dynamic method
            Type[] parameterTypes = {
            typeof(object),
            typeof(ExpressionContext),
            typeof(VariableCollection)
        };
            DynamicMethod dm = new DynamicMethod(DynamicMethodName, typeof(T), parameterTypes, _myOptions.OwnerType);
            return dm;
        }

        private void AddServices(IServiceContainer dest)
        {
            dest.AddService(typeof(ExpressionOptions), _myOptions);
            dest.AddService(typeof(ExpressionParserOptions), _myContext.ParserOptions);
            dest.AddService(typeof(ExpressionContext), _myContext);
            dest.AddService(typeof(IExpression), this);
            dest.AddService(typeof(ExpressionInfo), _myInfo);
        }

        /// <summary>
        /// Emit to an assembly. We've already computed long branches at this point,
        /// so we emit as a second pass
        /// </summary>
        /// <param name="ilg"></param>
        /// <param name="rootElement"></param>
        /// <param name="services"></param>
        private static void EmitToAssembly(FleeILGenerator ilg, ExpressionElement rootElement, IServiceContainer services)
        {
            AssemblyName assemblyName = new(EmitAssemblyName);

            string assemblyFileName = string.Format("{0}.dll", EmitAssemblyName);

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyFileName);

            MethodBuilder mb = moduleBuilder.DefineGlobalMethod("Evaluate", MethodAttributes.Public | MethodAttributes.Static, typeof(T), new Type[] {
            typeof(object),typeof(ExpressionContext),typeof(VariableCollection)});
            // already emitted once for local use,
            ilg.PrepareSecondPass(mb.GetILGenerator());

            rootElement.Emit(ilg, services);

            moduleBuilder.CreateGlobalFunctions();
            //assemblyBuilder.Save(assemblyFileName);
            assemblyBuilder.CreateInstance(assemblyFileName);
        }

        private void ValidateOwner(object owner)
        {
            Utility.AssertNotNull(owner, nameof(owner));
            if (_myOptions.OwnerType.IsAssignableFrom(owner.GetType()) == false)
            {
                string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.NewOwnerTypeNotAssignableToCurrentOwner);
                throw new ArgumentException(msg);
            }
        }

        public object Evaluate()
        {
            return _myEvaluator(_myOwner, _myContext, _myContext.Variables);
        }

        public T EvaluateGeneric()
        {
            return _myEvaluator(_myOwner, _myContext, _myContext.Variables);
        }
        T IGenericExpression<T>.Evaluate()
        {
            return EvaluateGeneric();
        }

        public IExpression Clone()
        {
            Expression<T> copy = (Expression<T>)MemberwiseClone();
            copy._myContext = _myContext.CloneInternal(true);
            copy._myOptions = copy._myContext.Options;
            return copy;
        }

        public override string ToString()
        {
            return _myExpression;
        }

        internal Type ResultType => _myOptions.ResultType;

        public string Text => _myExpression;

        public ExpressionInfo Info1 => _myInfo;

        ExpressionInfo IExpression.Info => Info1;

        public object Owner
        {
            get { return _myOwner; }
            set
            {
                ValidateOwner(value);
                _myOwner = value;
            }
        }

        public ExpressionContext Context => _myContext;
    }
}
