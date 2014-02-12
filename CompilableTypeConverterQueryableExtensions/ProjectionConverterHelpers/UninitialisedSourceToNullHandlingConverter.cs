using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverter.QueryableExtensions.ProjectionConverterHelpers
{
	/// <summary>
	/// Entity Framework won't let some values be set to null and some to non-null which is why the interim types have an is-initialised flag, so that this can
	/// be false for cases where it should be null. When translating from the interim type to the desired type, this needs to be respected; if the is-initialised
	/// flag on the interim type is false then it should be converted to null (or default(TDest), rather).
	/// </summary>
	public class UninitialisedSourceToNullHandlingConverter<TSource, TDest> : ICompilableTypeConverter<TSource, TDest>
	{
		private readonly ICompilableTypeConverter<TSource, TDest> _wrappedConverter;
		private readonly PropertyInfo _sourceTypeIsInitialisedProperty;
		private readonly Expression<Func<TSource, TDest>> _converterFuncExpression;
		private readonly Func<TSource, TDest> _converter;
		public UninitialisedSourceToNullHandlingConverter(ICompilableTypeConverter<TSource, TDest> wrappedConverter, PropertyInfo sourceTypeIsInitialisedProperty)
		{
			if (wrappedConverter == null)
				throw new ArgumentNullException("wrappedConverter");
			if (sourceTypeIsInitialisedProperty == null)
				throw new ArgumentNullException("destTypeIsInitialisedProperty");
			if (sourceTypeIsInitialisedProperty.DeclaringType != typeof(TSource))
				throw new ArgumentException("sourceTypeIsInitialisedProperty's DeclaringType must be TSource");
			if (sourceTypeIsInitialisedProperty.PropertyType != typeof(bool))
				throw new ArgumentException("sourceTypeIsInitialisedProperty's PropertyType must be bool");

			_wrappedConverter = wrappedConverter;
			_sourceTypeIsInitialisedProperty = sourceTypeIsInitialisedProperty;

			var srcParameter = Expression.Parameter(typeof(TSource), "src");
			_converterFuncExpression = Expression.Lambda<Func<TSource, TDest>>(
				GetTypeConverterExpression(srcParameter),
				srcParameter
			);

			_converter = _converterFuncExpression.Compile();
		}

		/// <summary>
		/// The ICompilableTypeConverter interface states that the param Expression value must be assignable to TSource
		/// </summary>
		public Expression GetTypeConverterExpression(Expression param)
		{
			if (param == null)
				throw new ArgumentNullException("param");

			return Expression.Condition(
				Expression.Property(param, _sourceTypeIsInitialisedProperty),
				_wrappedConverter.GetTypeConverterExpression(param),
				Expression.Constant(default(TDest), typeof(TDest))
			);
		}

		public Expression<Func<TSource, TDest>> GetTypeConverterFuncExpression()
		{
			return _converterFuncExpression;
		}

		public bool PassNullSourceValuesForProcessing
		{
			get
			{
				// This won't work with null source values since we expect to be able to check the isInitialised flag
				return false;
			}
		}

		public TDest Convert(TSource src)
		{
			return _converter(src);
		}

		public IEnumerable<PropertyMappingDetails> PropertyMappings
		{
			get { return _wrappedConverter.PropertyMappings; }
		}
	}
}
