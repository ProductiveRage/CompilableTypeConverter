using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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
    public class ListCompilablePropertyGetter<TSourceObject, TPropertyOnSourceElement, TPropertyAsRetrievedElement, TPropertyAsRetrieved>
        : AbstractGenericCompilablePropertyGetter<TSourceObject, TPropertyAsRetrieved>
    {
        private PropertyInfo _propertyInfo;
        private ICompilableTypeConverter<TPropertyOnSourceElement, TPropertyAsRetrievedElement> _typeConverter;
        public ListCompilablePropertyGetter(PropertyInfo propertyInfo, ICompilableTypeConverter<TPropertyOnSourceElement, TPropertyAsRetrievedElement> typeConverter)
        {
            // When checking types here, we're effectively only allowing equality - not IsAssignableFrom - of the elements of the IEnumerable data (since IEnumerable<B>
            // is not assignable to IEnumerable<A> even if B is derived from / implements A), this is desirable to ensure that the most appropriate property getter
            // ends up getting matched
            // - Validate propertyInfo
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            if (!propertyInfo.DeclaringType.Equals(typeof(TSourceObject)))
                throw new ArgumentException("Invalid propertyInfo - its DeclaringType must match TSourceObject");
            if (!typeof(IEnumerable<TPropertyOnSourceElement>).IsAssignableFrom(propertyInfo.PropertyType))
                throw new ArgumentException("Invalid propertyInfo - its PropertyType must be assignable match to IEnumerable<TPropertyOnSourceElement>");
            // - Validate typeparam combination
            if (!typeof(IEnumerable<TPropertyAsRetrievedElement>).IsAssignableFrom(typeof(TPropertyAsRetrieved)))
                throw new ArgumentException("Invalid configuration - TPropertyAsRetrieved must be assignable to IEnumerable<TPropertyAsRetrievedElement>");
            // - Validate typeConverter
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");

            _propertyInfo = propertyInfo;
            _typeConverter = typeConverter;
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
                throw new ArgumentException("param.Type must match typeparam TSourceObject");

            // Prepare an expression to retrieve the property from source object specified by the param argument
            var propertyValueParam = Expression.Property(
                param,
                _propertyInfo
            );

            // Pass this expression into the conversion generator (if null, return null, otherwise try convert the list data)
            return Expression.Condition(
                Expression.Equal(
                    propertyValueParam,
                    Expression.Constant(null)
                ),
                Expression.Constant(null, typeof(List<TPropertyAsRetrievedElement>)),
                getListConversionExpression(propertyValueParam)
            );
        }

        private Expression getListConversionExpression(Expression propertyValueParam)
        {
            if (propertyValueParam == null)
                throw new ArgumentNullException("propertyValueParam");
            if (!typeof(IEnumerable<TPropertyOnSourceElement>).IsAssignableFrom(propertyValueParam.Type))
                throw new ArgumentException("propertyValueParam.Type must match be assignable to IEnumerable<TPropertyOnSourceElement>");

            // Create local variable expressions
            var enumerator = Expression.Parameter(typeof(IEnumerator<TPropertyOnSourceElement>));
            var current = Expression.Parameter(typeof(TPropertyOnSourceElement));
            var results = Expression.Parameter(typeof(List<TPropertyAsRetrievedElement>));

            // Create a label to jump to from a loop and method body to describe loop
            var breakPoint = Expression.Label(typeof(List<TPropertyAsRetrievedElement>));
            return Expression.Block(

                // Pass local variables into scope (not current, that's only required in the inner-block)
                new[] { results, enumerator },

                // Initialise results list and retrieve enumerator from input data
                Expression.Assign(results, Expression.New(typeof(List<TPropertyAsRetrievedElement>).GetConstructor(new Type[0]))),
                Expression.Assign(enumerator, Expression.Call(propertyValueParam, typeof(IEnumerable<TPropertyOnSourceElement>).GetMethod("GetEnumerator"))),

                // Loop while enumerator.MoveNext returns true and add items to results list
                // - Jump to breakPoint when complete, passing results reference
                Expression.Loop(

                    Expression.IfThenElse(
                        Expression.IsTrue(Expression.Call(enumerator, typeof(IEnumerator).GetMethod("MoveNext"))),
                        Expression.Block(

                            // Get the current value from the enumerator, translate it using the type converter and add to the results list    
                            new[] { current },
                            Expression.Assign(current, Expression.Property(enumerator, "Current")),
                            Expression.Call(
                                results,
                                typeof(List<TPropertyAsRetrievedElement>).GetMethod("Add"),
                                _typeConverter.GetTypeConverterExpression(current)
                            )

                        ),
                        Expression.Break(breakPoint, results)
                    ),

                    // Pass break point reference to loop
                    breakPoint

                )
           );
        }
    }
}
