using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CompilableTypeConverter;
using CompilableTypeConverter.ConverterWrapperHelpers;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverterQueryableExtensions
{
    public static class IQueryable_Extensions
    {
        private static readonly Dictionary<AnonymousTypePropertyInfoSet, Type> _interimTypeCache;
        private static readonly Dictionary<Tuple<Type, Type>, object> _projectionDataTypeCache;
        static IQueryable_Extensions()
        {
            _interimTypeCache = new Dictionary<AnonymousTypePropertyInfoSet, Type>();
            _projectionDataTypeCache = new Dictionary<Tuple<Type, Type>, object>();
        }

        public static CompilableTypeConverterProjectionConfigurer<TSource> Project<TSource>(
            this IQueryable<TSource> source,
            ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if ((converterOverrideBehaviour != ConverterOverrideBehaviourOptions.ForceConverterRebuild)
            && (converterOverrideBehaviour != ConverterOverrideBehaviourOptions.IgnoreCache)
			&& (converterOverrideBehaviour != ConverterOverrideBehaviourOptions.UseAnyExistingConverter))
				throw new ArgumentOutOfRangeException("converterOverrideBehaviour");

            return new CompilableTypeConverterProjectionConfigurer<TSource>(source, converterOverrideBehaviour);
        }

        public class CompilableTypeConverterProjectionConfigurer<TSource>
        {
            private readonly IQueryable<TSource> _source;
            private readonly ConverterOverrideBehaviourOptions _converterOverrideBehaviour;
            public CompilableTypeConverterProjectionConfigurer(IQueryable<TSource> source, ConverterOverrideBehaviourOptions converterOverrideBehaviour)
            {
                if (source == null)
                    throw new ArgumentNullException("source");
                if ((converterOverrideBehaviour != ConverterOverrideBehaviourOptions.ForceConverterRebuild)
				&& (converterOverrideBehaviour != ConverterOverrideBehaviourOptions.IgnoreCache)
				&& (converterOverrideBehaviour != ConverterOverrideBehaviourOptions.UseAnyExistingConverter))
					throw new ArgumentOutOfRangeException("converterOverrideBehaviour");

                _source = source;
                _converterOverrideBehaviour = converterOverrideBehaviour;
            }

            public IEnumerable<TDest> To<TDest>()
            {
                // TODO: Expose a way to request that the Converter "alias" any custom settings (whether custom mappings or ignore requirements) for
                // the the anonymous type onto TDest? Limit to only those that are in the property list hat are required for the anonymous type?
                // - Not required for properties-to-ignore since they will already be accounted for (where applicable) in the TSource-to-TDest
                //   converter
                return GetProjection<TDest>()(_source);
            }

            private Func<IQueryable<TSource>, IEnumerable<TDest>> GetProjection<TDest>()
            {
                var lookupKey = Tuple.Create(typeof(TSource), typeof(TDest));
                lock (_projectionDataTypeCache)
                {
                    object projectionData;
                    if (_converterOverrideBehaviour != ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
                        projectionData = null;
                    else if (!_projectionDataTypeCache.TryGetValue(lookupKey, out projectionData))
                        projectionData = null;
                    if (projectionData == null)
                    {
                        // Generate a converter that would go straight from TSource to TDest
                        var converter = Converter.GetConverter<TSource, TDest>(
                            _converterOverrideBehaviour
                        );

                        // Generate a type that has only the properties from TSource that would be mapped to TDest
                        var interimType = GetInterimType(converter.SourcePropertiesAccessed);

                        // Generate a translator that will map TSource onto the interim type within the IQueryable (so that the minimum data can be retrieved
                        // by the IQueryable implementation) and then take an IEnumerable view of the data and map it onto TDest instances
                        projectionData = Activator.CreateInstance(
                            typeof(InterimProjectionData<,,>).MakeGenericType(new[]
					        {
						        typeof(TSource),
						        interimType,
						        typeof(TDest)
					        })
                        );
                        if (_converterOverrideBehaviour != ConverterOverrideBehaviourOptions.IgnoreCache)
                            _projectionDataTypeCache[lookupKey] = projectionData;
                    }
                    return ((IInterimProjectionData<TSource, TDest>)projectionData).GetTranslator();
                }
            }

            private Type GetInterimType(IEnumerable<PropertyInfo> properties)
            {
                if (properties == null)
                    throw new ArgumentNullException("properties");

                var interimTypePropertyData = new AnonymousTypePropertyInfoSet(properties);
                lock (_interimTypeCache)
                {
                    Type interimType;
                    if (_interimTypeCache.TryGetValue(interimTypePropertyData, out interimType))
                        return interimType;

                    interimType = AnonymousTypeCreator.DefaultInstance.Get(interimTypePropertyData);
                    _interimTypeCache[interimTypePropertyData] = interimType;
                    return interimType;
                }
            }
        }

        private class InterimProjectionData<TSource, TInterim, TDest> : IInterimProjectionData<TSource, TDest>
        {
            public Func<IQueryable<TSource>, IEnumerable<TDest>> GetTranslator()
            {
				// When calling the converter's GetTypeConverterFuncExpression method, we have to specify SkipNullHandling since Entity Framework
				// will give you a nasty error when trying to translate the expression into SQL if there's a branch that returns different data
				// depending upon input (something along the lines of "Additional information: Unable to create a null constant value of type
				// '<>AnonymousType-06c6364397b34c2a80eb2ad1c5305136'. Only entity types, enumeration types or primitive types are supported
				// in this context"). This means that an unhelpful exception will be thrown if any null objects are provided for translation
				// but hopefully this is not a common scenario (especially when retrieving from SQL - not that all IQueryables will have a
				// SQL origin - each row representation should be consistent and non-null).
				// TODO: https://groups.google.com/forum/#!topic/automapper-users/fsIQttgDksk suggests that the error would not occur with NH?
				// TODO: Test with a nested type that is nullable
				// TODO: Otherwise document limitations (eg. collections won't work due to Expression.Block)
				// TODO: TypeConverterExpressionNullBehaviourOptions has been moved
                var firstConverter = Converter.GetConverter<TSource, TInterim>();
                var secondConverter = Converter.GetConverter<TInterim, TDest>();
                return source => source
                    .Select(firstConverter.GetTypeConverterFuncExpression())
                    .AsEnumerable()
                    .Select(secondConverter.Convert);
            }
        }

        private interface IInterimProjectionData<TSource, TDest>
        {
            Func<IQueryable<TSource>, IEnumerable<TDest>> GetTranslator();
        }
    }
}
