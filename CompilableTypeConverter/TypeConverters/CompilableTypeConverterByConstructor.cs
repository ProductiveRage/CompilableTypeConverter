using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.ConstructorInvokers;
using CompilableTypeConverter.PropertyGetters.Compilable;

namespace CompilableTypeConverter.TypeConverters
{
    /// <summary>
    /// This will convert from one class to another using a constructor on the target type to pass in the data. The conversion process will be compiled using
    /// LINQ Expressions with the aim of the resulting code being comparable in speed to a hand-crafted version. Unlike the SimpleTypeConverterByConstructor,
    /// this does not use an IConstructorInvoker as the constructor calling code is tied to the implementation.
    /// </summary>
    public class CompilableTypeConverterByConstructor<TSource, TDest> : ICompilableTypeConverterByConstructor<TSource, TDest>
    {
        private ConstructorInfo _constructor;
        private List<ICompilablePropertyGetter> _propertyGetters;
        private Lazy<Func<TSource, TDest>> _converter;
        public CompilableTypeConverterByConstructor(IEnumerable<ICompilablePropertyGetter> propertyGetters, ConstructorInfo constructor)
        {
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
            if (constructor == null)
                throw new ArgumentNullException("constructor");

            // Ensure there are no null references in the property getter content
            var propertyGettersList = new List<ICompilablePropertyGetter>();
            foreach (var propertyGetter in propertyGetters)
            {
                if (propertyGetter == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
                if (!propertyGetter.SrcType.Equals(typeof(TSource)))
                    throw new ArgumentException("Encountered invalid SrcType in propertyGetters list, must match type param TSource");
                propertyGettersList.Add(propertyGetter);
            }

            // Ensure that the property getters correspond to the constructor that's being targetted
            var constructorParameters = constructor.GetParameters();
            if (propertyGettersList.Count != constructorParameters.Length)
                throw new ArgumentException("Number of propertyGetters.Count must match constructor.GetParameters().Length");
            for (var index = 0; index < propertyGettersList.Count; index++)
            {
                if (!constructorParameters[index].ParameterType.IsAssignableFrom(propertyGettersList[index].TargetType))
                    throw new ArgumentException("propertyGetter[" + index + "].TargetType is not assignable to corresponding constructor parameter type");
            }

            _constructor = constructor;
            _propertyGetters = propertyGettersList;
            _converter = new Lazy<Func<TSource, TDest>>(generateCompiledConverter, true);
        }

        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null
        /// </summary>
        public ConstructorInfo Constructor
        {
            get { return _constructor; }
        }

        /// <summary>
        /// Create a new target type instance from a source value - this will throw an exception if conversion fails
        /// </summary>
        public TDest Convert(TSource src)
        {
            return _converter.Value(src);
        }

        private Func<TSource, TDest> generateCompiledConverter()
        {
            // Declare an expression to represent the src parameter
            var srcParameter = Expression.Parameter(typeof(TSource), "src");

            // Return compiled expression that instantiates a new object by retrieving properties from the source and passing as constructor arguments
            return Expression.Lambda<Func<TSource, TDest>>(
                GetTypeConverterExpression(srcParameter),
                srcParameter
            ).Compile();
        }

        /// <summary>
        /// This must return a Linq Expression that returns a new TDest instance - the specified "param" Expression must have a type that is assignable to TSource.
        /// The resulting Expression will be assigned to a Lambda Expression typed as a TSource to TDest Func.
        /// </summary>
        public Expression GetTypeConverterExpression(Expression param)
        {
            if (param == null)
                throw new ArgumentNullException("param");
            if (!typeof(TSource).IsAssignableFrom(param.Type))
                throw new ArgumentException("param.Type must be assignable to typeparam TSource");

            // Instantiate expressions for each constructor parameter by using each of the property getters against the source value
            var constructorParameterExpressions = new List<Expression>();
            foreach (var constructorParameter in _constructor.GetParameters())
            {
                var index = constructorParameterExpressions.Count;
                constructorParameterExpressions.Add(
                    _propertyGetters[index].GetPropertyGetterExpression(param)
                );
            }

            // Return an expression that to instantiate a new TDest by using property getters as constructor arguments
            return Expression.Condition(
                Expression.Equal(
                    param,
                    Expression.Constant(null)
                ),
                Expression.Constant(default(TDest), typeof(TDest)),
                Expression.New(
                    _constructor,
                    constructorParameterExpressions.ToArray()
                )
            );
        }
    }
}
