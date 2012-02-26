using System;
using System.Collections.Generic;
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
        private List<ICompilablePropertyGetter> _propertyGetters;
        private List<PropertyInfo> _propertiesToSet;
        private Lazy<Func<TSource, TDest>> _converter;
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

            _propertyGetters = propertyGettersList;
            _propertiesToSet = propertiesToSetList;
            _converter = new Lazy<Func<TSource, TDest>>(generateCompiledConverter, true);
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
            for (var index = 0; index < _propertiesToSet.Count; index++)
            {
                newInstanceGenerationExpressions.Add(
                    Expression.Call(
                        dest,
                        _propertiesToSet[index].GetSetMethod(),
                        _propertyGetters[index].GetPropertyGetterExpression(param)
                    )
                );
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
    }
}
