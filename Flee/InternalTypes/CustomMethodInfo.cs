using System.Diagnostics;
using System.Reflection;
using Flee.ExpressionElements.Base;
using Flee.PublicTypes;

namespace Flee.InternalTypes
{
    /// <summary>
    /// Helper class to resolve overloads. Wraps a <see cref="MethodInfo"/> together with the
    /// metadata the resolver needs (param-array vs. extension classification, score, etc.).
    /// </summary>
    /// <param name="target">The candidate method being scored.</param>
    internal class CustomMethodInfo(MethodInfo target)
        : IComparable<CustomMethodInfo>, IEquatable<CustomMethodInfo>
    {
        /// <summary>
        /// The rating of how close the method matches the given arguments (0 is best).
        /// </summary>
        private float _myScore;

        /// <summary>
        /// Whether the candidate uses a <see cref="ParamArrayAttribute"/> tail parameter.
        /// </summary>
        public bool IsParamArray;

        /// <summary>
        /// The argument types matched against the fixed (non-paramArray) parameters.
        /// </summary>
        public Type[] MyFixedArgTypes = [];

        /// <summary>
        /// The argument types matched against the paramArray tail.
        /// </summary>
        public Type[] MyParamArrayArgTypes = [];

        /// <summary>
        /// Whether the candidate is being invoked as an extension method.
        /// </summary>
        public bool IsExtensionMethod;

        /// <summary>
        /// The element type of the paramArray, when applicable.
        /// </summary>
        public Type? ParamArrayElementType;

        /// <summary>
        /// Computes <see cref="_myScore"/> for this candidate against <paramref name="argTypes"/>.
        /// Lower scores are better.
        /// </summary>
        /// <param name="argTypes">The actual argument types from the call site.</param>
        public void ComputeScore(Type[] argTypes)
        {
            ParameterInfo[] @params = Target.GetParameters();

            _myScore = @params.Length == 0
                ? 0.0F
                : @params.Length == 1 && argTypes.Length == 0
                    ? 0.1F
                    : IsParamArray
                        ? ComputeScoreForParamArray(@params, argTypes)
                        : IsExtensionMethod
                            ? ComputeScoreExtensionMethodInternal(@params, argTypes)
                            : ComputeScoreInternal(@params, argTypes);
        }

        /// <summary>
        /// Compute a score showing how close our method matches the given argument types
        /// (for extension methods, where the first parameter is implicit).
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="argTypes">The actual argument types from the call site.</param>
        /// <returns>The score; lower is better.</returns>
        private float ComputeScoreExtensionMethodInternal(ParameterInfo[] parameters, Type[] argTypes)
        {
            Debug.Assert(parameters.Length == argTypes.Length + 1);
            int sum = 0;

            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                sum += ImplicitConverter.GetImplicitConvertScore(argTypes[i], parameters[i + 1].ParameterType);
            }

            return sum;
        }

        /// <summary>
        /// Compute a score showing how close our method matches the given argument types.
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="argTypes">The actual argument types from the call site.</param>
        /// <returns>The score; lower is better.</returns>
        private float ComputeScoreInternal(ParameterInfo[] parameters, Type[] argTypes)
        {
            // Our score is the average of the scores of each parameter.  The lower the score, the better the match.
            int sum = ComputeSum(parameters, argTypes);

            return sum / (float)argTypes.Length;
        }

        /// <summary>
        /// Sums the implicit-conversion scores for each parameter/argument pair.
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="argTypes">The actual argument types.</param>
        /// <returns>The total score.</returns>
        private static int ComputeSum(ParameterInfo[] parameters, Type[] argTypes)
        {
            Debug.Assert(parameters.Length == argTypes.Length);
            int sum = 0;

            for (int i = 0; i <= parameters.Length - 1; i++)
            {
                sum += ImplicitConverter.GetImplicitConvertScore(argTypes[i], parameters[i].ParameterType);
            }

            return sum;
        }

        /// <summary>
        /// Computes a score for a paramArray candidate, summing the fixed parameter scores plus
        /// the per-element scores against the paramArray's element type, with a small penalty.
        /// </summary>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="argTypes">The actual argument types.</param>
        /// <returns>The score; lower is better.</returns>
        private float ComputeScoreForParamArray(ParameterInfo[] parameters, Type[] argTypes)
        {
            ParameterInfo paramArrayParameter = parameters[parameters.Length - 1];
            int fixedParameterCount = paramArrayParameter.Position;

            ParameterInfo[] fixedParameters = new ParameterInfo[fixedParameterCount];

            Array.Copy(parameters, fixedParameters, fixedParameterCount);

            int fixedSum = ComputeSum(fixedParameters, MyFixedArgTypes);

            Type paramArrayElementType = paramArrayParameter.ParameterType.GetElementType()!;

            int paramArraySum = 0;

            foreach (Type argType in MyParamArrayArgTypes)
            {
                paramArraySum += ImplicitConverter.GetImplicitConvertScore(argType, paramArrayElementType);
            }

            float score = argTypes.Length > 0 ? (fixedSum + paramArraySum) / argTypes.Length : 0;

            // The param array score gets a slight penalty so that it scores worse than direct matches
            return score + 1;
        }

        /// <summary>
        /// Returns whether <paramref name="owner"/>'s access policy permits calling this method.
        /// </summary>
        /// <param name="owner">The element resolving the call.</param>
        /// <returns><see langword="true"/> when accessible.</returns>
        public bool IsAccessible(MemberElement owner)
        {
            return owner.IsMemberAccessible(Target);
        }

        /// <summary>
        /// Is the given <see cref="MethodInfo"/> usable as an overload for <paramref name="argTypes"/>?
        /// Sets <see cref="IsParamArray"/> and <see cref="IsExtensionMethod"/> when the match is
        /// of those special kinds.
        /// </summary>
        /// <param name="argTypes">The actual argument types.</param>
        /// <param name="previous">The element on the left of a member dereference, if any.</param>
        /// <param name="context">The compilation context (used for owner-based extension-method matching).</param>
        /// <returns><see langword="true"/> when this candidate is callable.</returns>
        public bool IsMatch(Type[] argTypes, MemberElement? previous, ExpressionContext context)
        {
            ParameterInfo[] parameters = Target.GetParameters();

            // If there are no parameters and no arguments were passed, then we are a match.
            if (parameters.Length == 0 & argTypes.Length == 0)
            {
                return true;
            }

            // If there are no parameters but there are arguments, we cannot be a match
            if (parameters.Length == 0 & argTypes.Length > 0)
            {
                return false;
            }

            // Is the last parameter a paramArray?
            ParameterInfo lastParam = parameters[parameters.Length - 1];

            if (!lastParam.IsDefined(typeof(ParamArrayAttribute), false))
            {
                //Extension method support
                if (parameters.Length == argTypes.Length + 1)
                {
                    IsExtensionMethod = true;
                    return AreValidExtensionMethodArgumentsForParameters(argTypes, parameters, previous, context);
                }
                if (parameters.Length != argTypes.Length)
                {
                    // Not a paramArray and parameter and argument counts don't match
                    return false;
                }
                else
                {
                    // Regular method call, do the test
                    return AreValidArgumentsForParameters(argTypes, parameters);
                }
            }

            // At this point, we are dealing with a paramArray call

            // If the parameter and argument counts are equal and there is an implicit conversion
            // from one to the other, we are a match.
            if (parameters.Length == argTypes.Length && AreValidArgumentsForParameters(argTypes, parameters))
            {
                return true;
            }
            else if (IsParamArrayMatch(argTypes, parameters, lastParam))
            {
                IsParamArray = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Verifies a paramArray match: fixed parameters take from the head of <paramref name="argTypes"/>,
        /// and the rest must each be implicitly convertible to the array element type.
        /// </summary>
        /// <param name="argTypes">The actual argument types.</param>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="paramArrayParameter">The trailing paramArray parameter.</param>
        /// <returns><see langword="true"/> when the call site matches via paramArray expansion.</returns>
        private bool IsParamArrayMatch(Type[] argTypes, ParameterInfo[] parameters, ParameterInfo paramArrayParameter)
        {
            // Get the count of arguments before the paramArray parameter
            int fixedParameterCount = paramArrayParameter.Position;
            Type[] fixedArgTypes = new Type[fixedParameterCount];
            ParameterInfo[] fixedParameters = new ParameterInfo[fixedParameterCount];

            // Get the argument types and parameters before the paramArray
            Array.Copy(argTypes, fixedArgTypes, fixedParameterCount);
            Array.Copy(parameters, fixedParameters, fixedParameterCount);

            // If the fixed arguments don't match, we are not a match
            if (!AreValidArgumentsForParameters(fixedArgTypes, fixedParameters))
            {
                return false;
            }

            // Get the type of the paramArray
            ParamArrayElementType = paramArrayParameter.ParameterType.GetElementType()!;

            // Get the types of the arguments passed to the paramArray
            Type[] paramArrayArgTypes = new Type[argTypes.Length - fixedParameterCount];
            Array.Copy(argTypes, fixedParameterCount, paramArrayArgTypes, 0, paramArrayArgTypes.Length);

            // Check each argument
            foreach (Type argType in paramArrayArgTypes)
            {
                if (!ImplicitConverter.EmitImplicitConvert(argType, ParamArrayElementType, null))
                {
                    return false;
                }
            }

            MyFixedArgTypes = fixedArgTypes;
            MyParamArrayArgTypes = paramArrayArgTypes;

            // They all match, so we are a match
            return true;
        }

        /// <summary>
        /// Verifies an extension-method match: the first parameter takes the receiver from
        /// <paramref name="previous"/> (or the context owner) and the rest must each be implicitly
        /// convertible from <paramref name="argTypes"/>.
        /// </summary>
        /// <param name="argTypes">The explicit argument types.</param>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <param name="previous">The element on the left of the dereference, if any.</param>
        /// <param name="context">The compilation context for owner-based fallback.</param>
        /// <returns><see langword="true"/> when the call site matches as an extension call.</returns>
        private static bool AreValidExtensionMethodArgumentsForParameters(
            Type[] argTypes,
            ParameterInfo[] parameters,
            MemberElement? previous,
            ExpressionContext context)
        {
            Debug.Assert(argTypes.Length + 1 == parameters.Length);

            if (previous != null)
            {
                if (!ImplicitConverter.EmitImplicitConvert(previous.ResultType, parameters[0].ParameterType, null))
                {
                    return false;
                }
            }
            else if (context.ExpressionOwner != null)
            {
                if (!ImplicitConverter.EmitImplicitConvert(
                    context.ExpressionOwner.GetType(),
                    parameters[0].ParameterType,
                    null))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            //Match if every given argument is implicitly convertible to the method's corresponding parameter
            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                if (!ImplicitConverter.EmitImplicitConvert(argTypes[i], parameters[i + 1].ParameterType, null))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Verifies a non-paramArray, non-extension match: each argument must implicitly convert
        /// to its corresponding parameter type.
        /// </summary>
        /// <param name="argTypes">The argument types.</param>
        /// <param name="parameters">The candidate's parameters.</param>
        /// <returns><see langword="true"/> when every position is convertible.</returns>
        private static bool AreValidArgumentsForParameters(Type[] argTypes, ParameterInfo[] parameters)
        {
            Debug.Assert(argTypes.Length == parameters.Length);
            // Match if every given argument is implicitly convertible to the method's corresponding parameter
            for (int i = 0; i <= argTypes.Length - 1; i++)
            {
                if (!ImplicitConverter.EmitImplicitConvert(argTypes[i], parameters[i].ParameterType, null))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compares two candidates by score; lower is better.
        /// </summary>
        /// <param name="other">The other candidate.</param>
        /// <returns>A negative/zero/positive integer following the <see cref="IComparable{T}"/> contract.</returns>
        public int CompareTo(CustomMethodInfo? other)
        {
            return other == null ? 1 : _myScore.CompareTo(other._myScore);
        }

        /// <summary>
        /// Internal helper that compares two candidates by score for equality.
        /// </summary>
        /// <param name="other">The other candidate.</param>
        /// <returns><see langword="true"/> when scores match.</returns>
        private bool Equals1(CustomMethodInfo? other)
        {
            return other != null && _myScore == other._myScore;
        }

        /// <summary>
        /// Explicit <see cref="IEquatable{T}.Equals(T)"/> implementation.
        /// </summary>
        /// <param name="other">The other candidate.</param>
        /// <returns><see langword="true"/> when scores match.</returns>
        bool IEquatable<CustomMethodInfo>.Equals(CustomMethodInfo? other)
        {
            return Equals1(other);
        }

        /// <summary>
        /// Gets the underlying <see cref="MethodInfo"/> being scored.
        /// </summary>
        public MethodInfo Target { get; } = target;
    }
}
