using System;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapperConstructor.TypeConverters;

namespace AutoMapperConstructor.PropertyGetters.Compilable
{
    /// <summary>
    /// Retrieve the property value of a specified object, converting the property value to another type using a particular compilable type converter
    /// </summary>
    /// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
    /// <typeparam name="TPropertyOnSource">This is the type that the property's is returned as before any conversion occurs</typeparam>
    /// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
    public class CompilableTypeConverterPropertyGetter<TSourceObject, TPropertyOnSource, TPropertyAsRetrieved>
        : AbstractGenericCompilablePropertyGetter<TSourceObject, TPropertyAsRetrieved>
    {
        private PropertyInfo _propertyInfo;
        private ICompilableTypeConverter<TPropertyOnSource, TPropertyAsRetrieved> _compilableTypeConverter;
        public CompilableTypeConverterPropertyGetter(PropertyInfo propertyInfo, ICompilableTypeConverter<TPropertyOnSource, TPropertyAsRetrieved> compilableTypeConverter)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            if (!propertyInfo.DeclaringType.Equals(typeof(TSourceObject)))
                throw new ArgumentException("Invalid propertyInfo - DeclaringType must match TSourceObject");
            if (!propertyInfo.PropertyType.Equals(typeof(TPropertyOnSource)))
                throw new ArgumentException("Invalid propertyInfo - PropertyType must match TPropertyOnSource");
            if (compilableTypeConverter == null)
                throw new ArgumentNullException("compilableTypeConverter");

            _propertyInfo = propertyInfo;
            _compilableTypeConverter = compilableTypeConverter;
        }
        
        public override PropertyInfo Property
        {
            get { return _propertyInfo; }
        }

        /// <summary>
        /// This must return a Linq Expression that retrieves the value from SrcType.Property as TargetType - the specified "param" Expression must have a type that
        /// is assignable to SrcType. The resulting Expression will be assigned to a Lambda Expression typed as a TSourceObject to TPropertyAsRetrieved Func.
        /// </summary>
        public override Expression GetPropertyGetterExpression(Expression param)
        {
            if (param == null)
                throw new ArgumentNullException("param");
            if (typeof(TSourceObject) != param.Type)
                throw new ArgumentException("param.NodeType must match typeparam TSourceObject");

            // Get property value (from object of type TSourceObject) without conversion (this will be as type TPropertyOnSource)
            // - If value is null, return default TPropertyAsRetrieved
            // - Otherwise, pass through type converter (to translate from TPropertyOnSource to TPropertyAsRetrieved)
            var propertyValue = Expression.Property(param, _propertyInfo);
            return Expression.Condition(
                Expression.Equal(
                    propertyValue,
                    Expression.Constant(null)
                ),
                Expression.Constant(default(TPropertyAsRetrieved), typeof(TPropertyAsRetrieved)),
                _compilableTypeConverter.GetTypeConverterExpression(propertyValue)
            );
        }
    }
}
