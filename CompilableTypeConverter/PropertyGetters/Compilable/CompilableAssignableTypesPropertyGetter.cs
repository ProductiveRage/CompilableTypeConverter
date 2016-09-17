using System;
using System.Linq.Expressions;
using System.Reflection;
using ProductiveRage.CompilableTypeConverter.Common;

namespace ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable
{
	/// <summary>
	/// Retrieves property values for class, attempting conversion to typeparam TPropertyAsRetrieved by calling Convert.ChangeType - compilable using Linq Expressions
	/// </summary>
	/// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
	/// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
	public class CompilableAssignableTypesPropertyGetter<TSourceObject, TPropertyAsRetrieved> : AbstractGenericCompilablePropertyGetter<TSourceObject, TPropertyAsRetrieved>
    {
        private PropertyInfo _propertyInfo;
        public CompilableAssignableTypesPropertyGetter(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
			if (!typeof(TSourceObject).HasProperty(propertyInfo))
				throw new ArgumentException("Invalid propertyInfo, not available on type TSource");

            _propertyInfo = propertyInfo;
        }

        public override PropertyInfo Property
        {
            get { return _propertyInfo; }
        }

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on
		/// the destination type will be assigned default(TPropertyAsRetrieved). For this implementation, this should not happen since the property values on the
		/// source object will be used to populate the property / constructor argument on the destination, but if the source reference is null then this will
		/// fail as soon as the property access is attempted.
		/// </summary>
		public override bool PassNullSourceValuesForProcessing { get { return false; } } 

        /// <summary>
        /// This must return a Linq Expression that retrieves the value from SrcType.Property as TargetType - the specified "param" Expression must have a type that
        /// is assignable to SrcType. The resulting Expression will be assigned to a Lambda Expression typed as a TSourceObject to TPropertyAsRetrieved Func.
        /// </summary>
        public override Expression GetPropertyGetterExpression(Expression param)
        {
            if (param == null)
                throw new ArgumentNullException("param");
            if (!typeof(TSourceObject).IsAssignableFrom(param.Type))
                throw new ArgumentException("param.Type must be assignable to typeparam TSourceObject");

            // Prepare to grab the property value from the source object directly
            Expression getter = Expression.Property(
                param,
                _propertyInfo
            );

            // Try to convert types if not directly assignable (eg. this covers some common enum type conversions)
            var targetType = typeof(TPropertyAsRetrieved);
            if (!targetType.IsAssignableFrom(_propertyInfo.PropertyType))
                getter = Expression.Convert(getter, targetType);

            // Perform boxing, if required (eg. when enum being handled and TargetType is object)
            if (!targetType.IsValueType && _propertyInfo.PropertyType.IsValueType)
                getter = Expression.TypeAs(getter, typeof(object));

            return getter;
        }
    }
}
