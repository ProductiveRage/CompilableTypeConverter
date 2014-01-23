using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.Common;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverter.PropertyGetters.Compilable
{
    /// <summary>
    /// Retrieve the property value of a specified object, converting the property value to another type using a particular compilable type converter - both the source
    /// and destination values must be IEnumerable where the element of the source and destination IEnumerable types relate to the source and destination types of the
    /// type converter
    /// </summary>
    /// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
    /// <typeparam name="TPropertyOnSourceElement">The property on the source type (before any conversion occurs) will be IEnumerable with elements of this type</typeparam>
    /// <typeparam name="TPropertyAsRetrievedElement">The type that the property will be retrieved as will be an IEnumerable with elements of this type</typeparam>
    /// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
    public class EnumerableCompilablePropertyGetter<TSourceObject, TPropertyOnSourceElement, TPropertyAsRetrievedElement, TPropertyAsRetrieved>
        : AbstractGenericCompilablePropertyGetter<TSourceObject, TPropertyAsRetrieved>
    {
        private readonly PropertyInfo _propertyInfo;
		private readonly ICompilableTypeConverter<TPropertyOnSourceElement, TPropertyAsRetrievedElement> _typeConverter;
        public EnumerableCompilablePropertyGetter(
			PropertyInfo propertyInfo,
			ICompilableTypeConverter<TPropertyOnSourceElement, TPropertyAsRetrievedElement> typeConverter,
			EnumerableSetNullHandlingOptions enumerableSetNullHandling)
        {
            // When checking types here, we're effectively only allowing equality - not IsAssignableFrom - of the elements of the IEnumerable data (since IEnumerable<B>
            // is not assignable to IEnumerable<A> even if B is derived from / implements A), this is desirable to ensure that the most appropriate property getter
            // ends up getting matched
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
			if (!typeof(TSourceObject).HasProperty(propertyInfo))
				throw new ArgumentException("Invalid propertyInfo, not available on type TSource");
			if (!typeof(IEnumerable<TPropertyOnSourceElement>).IsAssignableFrom(propertyInfo.PropertyType))
                throw new ArgumentException("Invalid propertyInfo - its PropertyType must be assignable match to IEnumerable<TPropertyOnSourceElement>");
            if (!typeof(IEnumerable<TPropertyAsRetrievedElement>).IsAssignableFrom(typeof(TPropertyAsRetrieved)))
                throw new ArgumentException("Invalid configuration - TPropertyAsRetrieved must be assignable to IEnumerable<TPropertyAsRetrievedElement>");
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");
			if (!Enum.IsDefined(typeof(EnumerableSetNullHandlingOptions), enumerableSetNullHandling))
				throw new ArgumentOutOfRangeException("enumerableSetNullHandling");

            _propertyInfo = propertyInfo;
            _typeConverter = typeConverter;
			EnumerableSetNullHandling = enumerableSetNullHandling;
        }

        public override PropertyInfo Property
        {
            get { return _propertyInfo; }
        }

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on
		/// the destination type will be assigned default(TPropertyAsRetrieved). For this implementation, this is true so that the decision whether or not to return
		/// null for the property value if the input is null is down to its EnumerableSetNullHandling value.
		/// </summary>
		public override bool PassNullSourceValuesForProcessing { get { return true; } }

		public EnumerableSetNullHandlingOptions EnumerableSetNullHandling { get; private set; }

        /// <summary>
        /// This must return a Linq Expression that retrieves the value from SrcType.Property as TargetType - the specified "param" Expression must have a type that
        /// is assignable to SrcType. The resulting Expression will be assigned to a Lambda Expression typed as a TSourceObject to TPropertyAsRetrieved Func.
        /// </summary>
        public override Expression GetPropertyGetterExpression(Expression param)
        {
            if (param == null)
                throw new ArgumentNullException("param");
            if (typeof(TSourceObject) != param.Type)
                throw new ArgumentException("param.Type must match typeparam TSourceObject");

            // Prepare an expression to retrieve the property from source object specified by the param argument
            var propertyValueParam = Expression.Property(
                param,
                _propertyInfo
            );

			var translatedSet = GetEnumerableConversionExpression(propertyValueParam);
			if (EnumerableSetNullHandling == EnumerableSetNullHandlingOptions.AssumeNonNullInput)
			{
				// If AssumeNonNullInput is specified then avoid the branch that handles the null case
				return translatedSet;
			}

            // Pass this expression into the conversion generator (if null, return null, otherwise try convert the list data)
            return Expression.Condition(
                Expression.Equal(
                    propertyValueParam,
                    Expression.Constant(null)
                ),
                Expression.Constant(null, typeof(IEnumerable<TPropertyAsRetrievedElement>)),
				translatedSet
            );
        }

        private Expression GetEnumerableConversionExpression(Expression propertyValueParam)
        {
            if (propertyValueParam == null)
                throw new ArgumentNullException("propertyValueParam");
            if (!typeof(IEnumerable<TPropertyOnSourceElement>).IsAssignableFrom(propertyValueParam.Type))
                throw new ArgumentException("propertyValueParam.Type must match be assignable to IEnumerable<TPropertyOnSourceElement>");

			// This translation (using LINQ's Select method) from IEnumerable<TPropertyOnSourceElement> to IEnumerable<TPropertyAsRetrievedElement> is supported
			// by Entity Framework and may be used for the translation of IQueryable results
			var selectMethod = typeof(Enumerable).GetMethods()
				.Select(m => new
				{
					Method = m,
					GenericArguments = m.GetGenericArguments(),
					Parameters = m.GetParameters()
				})
				.Where(m =>
					(m.Method.Name == "Select") &&
					(m.GenericArguments.Length == 2) &&
					(m.Parameters.Length == 2) &&
					(m.Parameters[0].ParameterType == typeof(IEnumerable<>).MakeGenericType(m.GenericArguments[0])) &&
					(m.Parameters[1].ParameterType == typeof(Func<,>).MakeGenericType(m.GenericArguments[0], m.GenericArguments[1]))
				)
				.Select(m => m.Method.MakeGenericMethod(typeof(TPropertyOnSourceElement), typeof(TPropertyAsRetrievedElement)))
				.Single();
			return Expression.Call(
				selectMethod,
				propertyValueParam,
				_typeConverter.GetTypeConverterFuncExpression()
			);
        }
    }
}
