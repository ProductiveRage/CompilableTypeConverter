using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
		private readonly IEnumerable<ICompilablePropertyGetter> _propertyGetters;
        private readonly Expression<Func<TSource, TDest>> _converterFuncExpression;
        private readonly Func<TSource, TDest> _converter;
        public CompilableTypeConverterByConstructor(
			IEnumerable<ICompilablePropertyGetter> propertyGetters,
			IEnumerable<ICompilableConstructorDefaultValuePropertyGetter> defaultValuePropertyGetters,
			ConstructorInfo constructor)
        {
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
			if (defaultValuePropertyGetters == null)
				throw new ArgumentNullException("defaultValuePropertyGetters");
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
			var defaultValuePropertyGettersList = new List<ICompilablePropertyGetter>();
			foreach (var defaultValuePropertyGetter in defaultValuePropertyGetters)
			{
				if (defaultValuePropertyGetter == null)
					throw new ArgumentException("Null reference encountered in defaultValuePropertyGetters list");
				if (defaultValuePropertyGetter.Constructor != constructor)
					throw new ArgumentException("Invalid reference encountered in defaultValuePropertyGetters set, does not match specified constructor");
				defaultValuePropertyGettersList.Add(defaultValuePropertyGetter);
			}

			// Combine the propertyGetters and defaultValuePropertyGetters into a single list that correspond to the constructor arguments
			// (ensuring that the property getters correspond to the constructor that's being targetted and that the numbers of property
			// getters is correct)
            var constructorParameters = constructor.GetParameters();
            if ((propertyGettersList.Count + defaultValuePropertyGettersList.Count) != constructorParameters.Length)
                throw new ArgumentException("Number of propertyGetters.Count must match constructor.GetParameters().Length");
			var combinedPropertyGetters = new List<ICompilablePropertyGetter>();
            for (var index = 0; index < constructorParameters.Length; index++)
            {
				var constructorParameter = constructorParameters[index];
				var defaultValuePropertyGetter = defaultValuePropertyGetters.FirstOrDefault(p => p.ArgumentName == constructorParameter.Name);
				if (defaultValuePropertyGetter != null)
				{
					// There's no validation to perform here, the IConstructorDefaultValuePropertyGetter interface states that the TargetType
					// will match the named constructor argument that it relates to
					combinedPropertyGetters.Add(defaultValuePropertyGetter);
					continue;
				}

				// If there was no default value property getter, then the first entry in the propertyGetters set should correspond to the
				// current constructor argument (since we keep removing the first item in that set when a match is found, this remains true
				// as we process multiple arguments)
				if (propertyGettersList.Count == 0)
					throw new ArgumentException("Unable to match a property getter to constructor argument \"" + constructorParameter.Name + "\"");
				var propertyGetter = propertyGettersList[0];
				if (!constructorParameter.ParameterType.IsAssignableFrom(propertyGetter.TargetType))
					throw new ArgumentException("propertyGetter[" + index + "].TargetType is not assignable to corresponding constructor parameter type");
				combinedPropertyGetters.Add(propertyGetter);
				propertyGettersList.RemoveAt(0);
            }

			// Record the validated member variables
			_propertyGetters = combinedPropertyGetters.AsReadOnly();
			Constructor = constructor;
			NumberOfConstructorArgumentsMatchedWithNonDefaultValues = constructorParameters.Length - defaultValuePropertyGettersList.Count;

			// Generate a Expression<Func<TSource, TDest>>, the _rawConverterExpression is still required for the GetTypeConverterExpression
			// method (this may be called to retrieve the raw expression, rather than the Func-wrapped version - eg. by the ListCompilablePropertyGetter,
			// which has a set of TSource objects and wants to translate them into a set of TDest objects)
			var srcParameter = Expression.Parameter(typeof(TSource), "src");
			_converterFuncExpression = Expression.Lambda<Func<TSource, TDest>>(
				GetTypeConverterExpression(srcParameter),
				srcParameter
			);

			// Compile the expression into an actual Func<TSource, TDest> (this is expected to be the most commonly-used form of the data)
			_converter = _converterFuncExpression.Compile();
        }

        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null
        /// </summary>
        public ConstructorInfo Constructor { get; private set; }

		/// <summary>
		/// This will always be zero or greater and less than or equal to the number of parameters that the Constructor reference has
		/// </summary>
		public int NumberOfConstructorArgumentsMatchedWithNonDefaultValues { get; private set; }

        /// <summary>
        /// Create a new target type instance from a source value - this will throw an exception if conversion fails
        /// </summary>
        public TDest Convert(TSource src)
        {
            return _converter(src);
        }

        /// <summary>
        /// This must return a Linq Expression that returns a new TDest instance - the specified "param" Expression must have a type that is assignable to TSource.
        /// The resulting Expression may be used to create a Func to take a TSource instance and return a new TDest if the specified param is a ParameterExpression.
		/// If an expression of this form is required then the GetTypeConverterFuncExpression method may be more appropriate to use, this method is only when direct
		/// access to the conversion expression is required, it may be preferable to GetTypeConverterFuncExpression when generating complex expression that this is
		/// to be part of, potentially gaining a minor performance improvement (compared to calling GetTypeConverterFuncExpression) at the cost of compile-time
		/// type safety. Alternatively, this method may be required if an expression value is to be convered where the expression is not a ParameterExpression.
		/// </summary>
        public Expression GetTypeConverterExpression(Expression param)
        {
            if (param == null)
                throw new ArgumentNullException("param");
            if (!typeof(TSource).IsAssignableFrom(param.Type))
                throw new ArgumentException("param.Type must be assignable to typeparam TSource");

            // Return an expression that to instantiate a new TDest by using property getters as constructor arguments
            return Expression.Condition(
                Expression.Equal(
                    param,
                    Expression.Constant(null)
                ),
                Expression.Constant(default(TDest), typeof(TDest)),
                Expression.New(
                    Constructor,
					_propertyGetters.Select(propertyGetter => propertyGetter.GetPropertyGetterExpression(param))
                )
            );
		}

		/// <summary>
		/// This will never return null, it will return an Func Expression for mapping from a TSource instance to a TDest
		/// </summary>
		public Expression<Func<TSource, TDest>> GetTypeConverterFuncExpression()
		{
			return _converterFuncExpression;
		}
	}
}
