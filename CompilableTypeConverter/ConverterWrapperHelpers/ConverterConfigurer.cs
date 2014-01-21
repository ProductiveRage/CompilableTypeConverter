using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CompilableTypeConverter.ConverterWrapperHelpers
{
	public class ConverterConfigurer<TSource, TDest>
	{
		private readonly IEnumerable<PropertyInfo> _propertiesToIgnore;
		public ConverterConfigurer(IEnumerable<PropertyInfo> propertiesToIgnore)
		{
			if (propertiesToIgnore == null)
				throw new ArgumentNullException("propertiesToIgnore");

			_propertiesToIgnore = propertiesToIgnore.ToList().AsReadOnly();
			if (_propertiesToIgnore.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in propertiesToIgnore set");
		}

		/// <summary>
		/// Specify properties on the TSource type that should be ignored if the generated converter uses the populate-by-property-setting
		/// method (this will not have any effect if the by-constructor method seems most appropriate when the converter is created). The
		/// accessor must all indicate properties on TDest that are publicly writeable and non-indexed, anything else will result in an
		/// exception being thrown.
		/// </summary>
		public ConverterConfigurer<TSource, TDest> Ignore(params Expression<Func<TDest, object>>[] accessors)
		{
			if (accessors == null)
				throw new ArgumentNullException("accessors");

			var propertyInfoList = new List<PropertyInfo>();
			foreach (var accessor in accessors)
			{
				if (accessor == null)
					throw new ArgumentException("Null reference encountered in accessors set");

				propertyInfoList.Add(
					GetPropertyFromAccessorFuncExpression(accessor)
				);
			}
			if (!propertyInfoList.Any())
				return this;
			return new ConverterConfigurer<TSource, TDest>(_propertiesToIgnore.Concat(propertyInfoList));
		}

		/// <summary>
		/// This will throw an exception if a converter for the TSource, TDest could not be created
		/// </summary>
		public void Create(ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			Converter.CreateMap<TSource, TDest>(_propertiesToIgnore, converterOverrideBehaviour);
		}

		/// <summary>
		/// This will throw an exception for a null accessor reference, one that is not a member accessor, one that is not a property member accessor
		/// or one whose target property is either not public writeable or is indexed
		/// </summary>
		private PropertyInfo GetPropertyFromAccessorFuncExpression(Expression<Func<TDest, object>> accessor)
		{
			// This code was very much inspired by looking into the AutoMapper source! :)
			if (accessor == null)
				throw new ArgumentNullException("accessor");

			// Expecting the accessor.Body to be a MemberExpression indicating a Property, but if the target Property type needs to be boxed then
			// it will be wrapped in a UnaryExpression
			Expression accessorBody = accessor.Body;
			if (accessorBody.NodeType == ExpressionType.Convert)
			{
				var convertExpression = accessor.Body as UnaryExpression;
				accessorBody = convertExpression.Operand;
			}
			if (accessorBody.NodeType != ExpressionType.MemberAccess)
				throw new ArgumentException("accessor.Body.NodeType must be a MemberAccess");
			var property = (accessorBody as MemberExpression).Member as PropertyInfo;
			if (property == null)
				throw new ArgumentException("The accessor must specify a property");
			if (property.GetSetMethod() == null)
				throw new ArgumentException("The accessor must specify a writeable property");
			if (property.GetIndexParameters().Any())
				throw new ArgumentException("The accessor must specify a non-indexed property");
			return property;
		}
	}
}
