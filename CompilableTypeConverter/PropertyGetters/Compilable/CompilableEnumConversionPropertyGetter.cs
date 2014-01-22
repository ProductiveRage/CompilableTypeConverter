using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CompilableTypeConverter.NameMatchers;

namespace CompilableTypeConverter.PropertyGetters.Compilable
{
    /// <summary>
    /// Retrieves property values for class, converting from the property type on the source object to that of a destination - this property getter only supports enums
    /// as the source and target property types, it attempts to translate by name for any matches and will default to straight Expression.Convert calls if no matches
    /// are found. Note: This could result in unexpected behaviour if the value corresponding to an unmatched name on the source enum is in use on the destination
    /// enum, or if the source is an uint enum while the destination is an int - some numeric wrapping can occur.
    /// </summary>
    /// <typeparam name="TSourceObject">This is the type of the target object, whose property is to be retrieved</typeparam>
    /// <typeparam name="TPropertyAsRetrieved">This is the type that the property's value will be returned as</typeparam>
    public class CompilableEnumConversionPropertyGetter<TSourceObject, TPropertyAsRetrieved> : AbstractGenericCompilablePropertyGetter<TSourceObject, TPropertyAsRetrieved>
    {
        private PropertyInfo _propertyInfo;
        private INameMatcher _enumNameMatcher;
        public CompilableEnumConversionPropertyGetter(PropertyInfo propertyInfo, INameMatcher enumNameMatcher)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            if (!propertyInfo.DeclaringType.Equals(typeof(TSourceObject)))
                throw new ArgumentException("Invalid propertyInfo - DeclaringType must match TSourceObject");
            if (!propertyInfo.PropertyType.IsEnum)
                throw new Exception("Invalid propertyInfo.PropertyType - must have IsEnum true");
            if (enumNameMatcher == null)
                throw new ArgumentNullException("enumNameMatcher");

            if (!typeof(TPropertyAsRetrieved).IsEnum)
                throw new Exception("typeparam TPropertyAsRetrieved must report IsEnum true");

            _propertyInfo = propertyInfo;
            _enumNameMatcher = enumNameMatcher; 
        }

        public override PropertyInfo Property
        {
            get { return _propertyInfo; }
        }

		/// <summary>
		/// If the source value is null should this property getter still be processed? If not, the assumption is that the target property / constructor argument on
		/// the destination type will be assigned default(TPropertyAsRetrieved). For this implementation, this should not happen since an exception would be raised
		/// as soon as an attempt was made to access the enum property on the source reference if the source reference is null.
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

            // Retrieve value of property from source
            var value = Expression.Property(param, _propertyInfo);

            // If no lookups match, just try a straight conversion using the underlying numeric value
            Expression getter = Expression.Convert(value, typeof(TPropertyAsRetrieved));

            // For each lookup, wrap the current body expression in an if statement - handling the current lookup pair if it matches and defaulting to
            // whatever was in body if not
            var lookups = generateLookups();
            foreach (var key in lookups.Keys)
            {
                getter = Expression.Condition(
                    Expression.Equal(
                        Expression.Constant(key),
                        value
                    ),
                    Expression.Constant(lookups[key], typeof(TPropertyAsRetrieved)),
                    getter
                );
            }
            return getter;
        }

        /// <summary>
        /// Generate a lookup for all source enum value to dest enum values mappings that we can (according to the enumNameMatcher)
        /// </summary>
        private Dictionary<object, TPropertyAsRetrieved> generateLookups()
        {
            var lookups = new Dictionary<object, TPropertyAsRetrieved>();
            var srcNames = Enum.GetNames(_propertyInfo.PropertyType);
            var destNames = Enum.GetNames(typeof(TPropertyAsRetrieved));
            foreach (var srcName in srcNames)
            {
                var destName = destNames.FirstOrDefault(n => _enumNameMatcher.IsMatch(srcName, n));
                if (destName != null)
                {
                    var srcValue = Enum.Parse(_propertyInfo.PropertyType, srcName);
                    var destValue = Enum.Parse(typeof(TPropertyAsRetrieved), destName);
                    lookups.Add(srcValue, (TPropertyAsRetrieved)destValue);
                }
            }
            return lookups;
        }
    }
}
