using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProductiveRage.CompilableTypeConverter.ConstructorPrioritisers.Factories;
using ProductiveRage.CompilableTypeConverter.NameMatchers;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Factories;
using ProductiveRage.CompilableTypeConverter.TypeConverters;
using ProductiveRage.CompilableTypeConverter.TypeConverters.Factories;

namespace ProductiveRage.CompilableTypeConverter.ConverterWrapperHelpers
{
	/// <summary>
	/// This class is a configuration of the compilable type converter, intended for common cases. This class mutates its own state as new conversions become
	/// available (or when the Reset method is called) but is not thread safe. If thread-safe access is required then this must be handled by the caller (this
	/// is done in the case of the static Converter class).
	/// </summary>
	public class ConverterWrapper
	{
		private ExtendableCompilableTypeConverterFactory _constructorBasedConverterFactory;
		private ExtendableCompilableTypeConverterFactory _propertySetterBasedConverterFactory;
		private readonly ByPropertySettingNullSourceBehaviourOptions _nullSourceBehaviour;
		private readonly EnumerableSetNullHandlingOptions _enumerableSetNullHandling;
		private readonly List<PropertyInfo> _allPropertiesToIgnoreToPropertySetterConversions;
		private readonly List<PropertyInfo> _allInitialisedFlagsIfTranslatingNullsToEmptyInstances;
		private readonly Dictionary<Tuple<Type, Type>, object> _converterCache;
		public ConverterWrapper(ByPropertySettingNullSourceBehaviourOptions nullSourceBehaviour, EnumerableSetNullHandlingOptions enumerableSetNullHandling)
		{
			if (!Enum.IsDefined(typeof(ByPropertySettingNullSourceBehaviourOptions), nullSourceBehaviour))
				throw new ArgumentOutOfRangeException("nullSourceBehaviour");
			if (!Enum.IsDefined(typeof(EnumerableSetNullHandlingOptions), enumerableSetNullHandling))
				throw new ArgumentOutOfRangeException("enumerableSetNullHandling");

			_nullSourceBehaviour = nullSourceBehaviour;
			_enumerableSetNullHandling = enumerableSetNullHandling;
			_allPropertiesToIgnoreToPropertySetterConversions = new List<PropertyInfo>();
			_allInitialisedFlagsIfTranslatingNullsToEmptyInstances = new List<PropertyInfo>();
			_converterCache = new Dictionary<Tuple<Type, Type>, object>();

			// Prepare converter factories (for by-constructor and by-property-setters) using the base types (AssignableType and
			// EnumConversion property getter factories)
			var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
			var basePropertyGetterFactories = new ICompilablePropertyGetterFactory[]
			{
				new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
				new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
			};
			_constructorBasedConverterFactory = ExtendableCompilableTypeConverterFactoryHelpers.GenerateConstructorBasedFactory(
				nameMatcher,
				new ArgsLengthTypeConverterPrioritiserFactory(),
				basePropertyGetterFactories,
				_enumerableSetNullHandling
			);
			_propertySetterBasedConverterFactory = ExtendableCompilableTypeConverterFactoryHelpers.GeneratePropertySetterBasedFactory(
				nameMatcher,
				CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions.MatchAll,
				basePropertyGetterFactories,
				_allPropertiesToIgnoreToPropertySetterConversions,
				_nullSourceBehaviour,
				_allInitialisedFlagsIfTranslatingNullsToEmptyInstances,
				_enumerableSetNullHandling
			);
		}

		/// <summary>
		/// This will throw an exception if unable to generate a converter for request TSource and TDest pair, it will never return null
		/// </summary>
		public ICompilableTypeConverter<TSource, TDest> GetConverter<TSource, TDest>(
			IEnumerable<PropertyInfo> propertiesToIgnoreIfSettingPropertiesOnTDest,
			IEnumerable<PropertyInfo> initialisedFlagsIfTranslatingNullsToEmptyInstances,
			ConverterOverrideBehaviourOptions converterOverrideBehaviour)
		{
			if (propertiesToIgnoreIfSettingPropertiesOnTDest == null)
				throw new ArgumentNullException("propertiesToIgnoreIfSettingPropertiesOnTDest");
			var propertiesToIgnoreList = propertiesToIgnoreIfSettingPropertiesOnTDest.ToList();
			if (propertiesToIgnoreList.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in propertiesToIgnoreIfSettingPropertiesOnTDest set ");
			if (initialisedFlagsIfTranslatingNullsToEmptyInstances == null)
				throw new ArgumentNullException("initialisedFlagsIfTranslatingNullsToEmptyInstances");
			var initialisedFlagsIfTranslatingNullsToEmptyInstancesList = initialisedFlagsIfTranslatingNullsToEmptyInstances.ToList();
			if (initialisedFlagsIfTranslatingNullsToEmptyInstances.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in initialisedFlagsIfTranslatingNullsToEmptyInstances set ");
			if (!Enum.IsDefined(typeof(ConverterOverrideBehaviourOptions), converterOverrideBehaviour))
				throw new ArgumentOutOfRangeException("converterOverrideBehaviour");

			var cacheKey = Tuple.Create(typeof(TSource), typeof(TDest));
			if (converterOverrideBehaviour != ConverterOverrideBehaviourOptions.IgnoreCache)
			{
				object unTypedCachedResult;
				if (_converterCache.TryGetValue(cacheKey, out unTypedCachedResult))
				{
					if (converterOverrideBehaviour == ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
						return (ICompilableTypeConverter<TSource, TDest>)unTypedCachedResult;
					_converterCache.Remove(cacheKey);
				}
			}

			// If there are any properties-to-ignore specified then add them to the total combined list and re-generate the
			// _propertySetterBasedConverterFactory reference to take them into account. Also add any initialised-flags to the ignore list
			// since these are not expected to be mapped from the source data, these flags are expected to be set afterwards (with false
			// if the source reference was null and true if not)
			if (propertiesToIgnoreList.Any() || initialisedFlagsIfTranslatingNullsToEmptyInstances.Any())
			{
				_allPropertiesToIgnoreToPropertySetterConversions.AddRange(propertiesToIgnoreIfSettingPropertiesOnTDest);
				_allPropertiesToIgnoreToPropertySetterConversions.AddRange(initialisedFlagsIfTranslatingNullsToEmptyInstances);
				_allInitialisedFlagsIfTranslatingNullsToEmptyInstances.AddRange(initialisedFlagsIfTranslatingNullsToEmptyInstances);
				var currentByPropertySetterConvererFactoryConfigurationData = _propertySetterBasedConverterFactory.GetConfigurationData();
				_propertySetterBasedConverterFactory = new ExtendableCompilableTypeConverterFactory(
					currentByPropertySetterConvererFactoryConfigurationData.NameMatcher,
					currentByPropertySetterConvererFactoryConfigurationData.BasePropertyGetterFactories,
					propertyGetterFactories => new CompilableTypeConverterByPropertySettingFactory(
						new CombinedCompilablePropertyGetterFactory(propertyGetterFactories),
						CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions.MatchAll,
						_allPropertiesToIgnoreToPropertySetterConversions,
						_nullSourceBehaviour,
						_allInitialisedFlagsIfTranslatingNullsToEmptyInstances
					),
					currentByPropertySetterConvererFactoryConfigurationData.PropertyGetterFactoryExtrapolator
				);
			}

			// Attempt to generate a converter using the _constructorBasedConverterFactory and then the _propertySetterBasedConverterFactory.
			// If both fail then it's difficult to know for sure which exception is most useful to allow through. If the destination type has
			// a parameter-less constructor then allow the by-property-setter exception to be raised (if there was no parameter-less constructor
			// then the by-property-setter factory wouldn't have been able to do anything and we'll assume that the by-constructor factory
			// exception is more useful).
			ICompilableTypeConverter<TSource, TDest> converter;
			Exception mappingException;
			try
			{
				converter = _constructorBasedConverterFactory.Get<TSource, TDest>();
			}
			catch (Exception byConstructorException)
			{
				try
				{
					converter = _propertySetterBasedConverterFactory.Get<TSource, TDest>();
				}
				catch (Exception byPropertySettingException)
				{
					if (typeof(TDest).GetConstructor(new Type[0]) != null)
						mappingException = byPropertySettingException;
					else
						mappingException = byConstructorException;
					throw mappingException;
				}
			}

			_constructorBasedConverterFactory = _constructorBasedConverterFactory.AddNewConverter(converter);
			_propertySetterBasedConverterFactory = _propertySetterBasedConverterFactory.AddNewConverter(converter);
			if (converterOverrideBehaviour != ConverterOverrideBehaviourOptions.IgnoreCache)
				_converterCache[cacheKey] = converter;
			return converter;
		}

		/// <summary>
		/// Specify a converter that will used whenever either a TSource-to-TDest translation is requested or a translation where properties must
		/// be translated from TSource to TDest (or from IEnumerable sets of TSource to TDest). Any converter that was previously available for
		/// this translation will be superceded by the one specified here (subsequent calls to this method with the same TSource and TDest
		/// will then take precedence again).
		/// </summary>
		public void SetConverter<TSource, TDest>(ICompilableTypeConverter<TSource, TDest> converter)
		{
			if (converter == null)
				throw new ArgumentNullException("converter");

			// For the ExtendableCompilableTypeConverterFactories, we just call AddNewConverter which will register the new converter before
			// any other converters which may handle the same translation. This sidesteps any complicated logic that might be involved in
			// trying to identify any converters from its repertoire that may need to be removed, this way the new converter will just
			// take precedence and any others will be ignored.
			_constructorBasedConverterFactory = _constructorBasedConverterFactory.AddNewConverter<TSource, TDest>(converter);
			_propertySetterBasedConverterFactory = _propertySetterBasedConverterFactory.AddNewConverter<TSource, TDest>(converter);

			// For the internal cache (which will handle requests for translations of TSource-to-TDest, as opposed to translations which
			// require property retrieval from TSource to populate a property of constructor argument of TDest), we just set the value
			// on the dictionary, overwriting anything there before or adding a new item if not overwriting anything pre-existing.
			_converterCache[Tuple.Create(typeof(TSource), typeof(TDest))] = converter;
		}
	}
}
