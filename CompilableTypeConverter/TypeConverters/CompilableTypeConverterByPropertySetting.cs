using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.PropertyGetters.Compilable;

namespace CompilableTypeConverter.TypeConverters
{
    /// <summary>
    /// This will convert from one class to another by instantiating the target through a parameterless constructor and then setting individual properties. The
    /// conversion process will be compiled using LINQ Expressions with the aim of the resulting code being comparable in speed to a hand-crafted version.
    /// </summary>
    public class CompilableTypeConverterByPropertySetting<TSource, TDest> : ICompilableTypeConverter<TSource, TDest> where TDest : new()
    {
        private readonly IEnumerable<ICompilablePropertyGetter> _propertyGetters;
		private readonly IEnumerable<PropertyInfo> _propertiesToSet;
		private readonly Expression<Func<TSource, TDest>> _converterFuncExpression;
		private readonly Func<TSource, TDest> _converter;
		public CompilableTypeConverterByPropertySetting(IEnumerable<ICompilablePropertyGetter> propertyGetters, IEnumerable<PropertyInfo> propertiesToSet)
        {
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
            if (propertiesToSet == null)
                throw new ArgumentNullException("propertiesToSet");

            // Ensure there are no null references in the property lists
            var propertyGettersList = new List<ICompilablePropertyGetter>();
            foreach (var propertyGetter in propertyGetters)
            {
                if (propertyGetter == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
                if (!propertyGetter.SrcType.Equals(typeof(TSource)))
                    throw new ArgumentException("Encountered invalid SrcType in propertyGetters list, must match type param TSource");
                propertyGettersList.Add(propertyGetter);
            }
            var propertiesToSetList = new List<PropertyInfo>();
            foreach (var property in propertiesToSet)
            {
                if (property == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
                if (!property.DeclaringType.Equals(typeof(TDest)))
                    throw new ArgumentException("Encountered invalid DeclaringType in property list, must match type param TDest");
                propertiesToSetList.Add(property);
            }

            // Ensure that the property getters correspond to the target properties
            if (propertyGettersList.Count != propertiesToSetList.Count)
                throw new ArgumentException("Number of propertyGetters specified must match number of propertiesToSet");
            for (var index = 0; index < propertyGettersList.Count; index++)
            {
                if (!propertiesToSetList[index].PropertyType.IsAssignableFrom(propertyGettersList[index].TargetType))
                    throw new ArgumentException("propertyGetter[" + index + "].TargetType is not assignable to corresponding propertyToSet");
            }

			_propertyGetters = propertyGettersList.AsReadOnly();
            _propertiesToSet = propertiesToSetList.AsReadOnly();

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
		/// This will never be null nor contain any null references
		/// </summary>
		public IEnumerable<PropertyInfo> SourcePropertiesAccessed { get { return _propertyGetters.Select(p => p.Property); } }

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

            // Declare a local variable that will be used within the Expression block to have a new instance assigned to it and properties set
            var dest = Expression.Parameter(typeof(TDest));

            // Define statements to instantiate new value, set properties and then return the reference
            var newInstanceGenerationExpressions = new List<Expression>
			{
                Expression.Assign(
                    dest,
                    Expression.New(typeof(TDest).GetConstructor(new Type[0]))
                )
            };
			var index = 0;
			foreach (var propertyToSet in _propertiesToSet)
            {
                newInstanceGenerationExpressions.Add(
                    Expression.Call(
                        dest,
						propertyToSet.GetSetMethod(),
						_propertyGetters.ElementAt(index).GetPropertyGetterExpression(param)
					)
				);
				index++;
			}
            newInstanceGenerationExpressions.Add(
                dest
            );

			// Return an expression that to instantiate a new TDest by using property getters as constructor arguments
			// - If source is null, return default(TDest)
			return Expression.Condition(
				Expression.Equal(
					param,
					Expression.Constant(null)
				),
				Expression.Constant(default(TDest), typeof(TDest)),
                Expression.Block(
                    new[] { dest },
                    newInstanceGenerationExpressions
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
