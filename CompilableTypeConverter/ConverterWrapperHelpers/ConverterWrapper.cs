using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Compilable;
using CompilableTypeConverter.PropertyGetters.Factories;
using CompilableTypeConverter.TypeConverters;
using CompilableTypeConverter.TypeConverters.Factories;

namespace CompilableTypeConverter.ConverterWrapperHelpers
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
				basePropertyGetterFactories
			);
			_propertySetterBasedConverterFactory = ExtendableCompilableTypeConverterFactoryHelpers.GeneratePropertySetterBasedFactory(
				nameMatcher,
				CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions.MatchAll,
				basePropertyGetterFactories,
				new PropertyInfo[0],
				_nullSourceBehaviour,
				_enumerableSetNullHandling
			);
		}

		/// <summary>
		/// This will throw an exception if unable to generate a converter for request TSource and TDest pair, it will never return null
		/// </summary>
		public ICompilableTypeConverter<TSource, TDest> GetConverter<TSource, TDest>(
			IEnumerable<PropertyInfo> propertiesToIgnoreIfSettingPropertiesOnTDest,
			ConverterOverrideBehaviourOptions converterOverrideBehaviour)
		{
			if (propertiesToIgnoreIfSettingPropertiesOnTDest == null)
				throw new ArgumentNullException("propertiesToIgnoreIfSettingPropertiesOnTDest");
			var propertiesToIgnoreList = propertiesToIgnoreIfSettingPropertiesOnTDest.ToList();
			if (propertiesToIgnoreList.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in propertiesToIgnoreIfSettingPropertiesOnTDest set ");
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

			// If there are any properties-to-ignore specified then add them to the total combined list and re-generate
			// the _propertySetterBasedConverterFactory reference to take them into account
			if (propertiesToIgnoreList.Any())
			{
				_allPropertiesToIgnoreToPropertySetterConversions.AddRange(propertiesToIgnoreIfSettingPropertiesOnTDest);
				var currentByPropertySetterConvererFactoryConfigurationData = _propertySetterBasedConverterFactory.GetConfigurationData();
				_propertySetterBasedConverterFactory = new ExtendableCompilableTypeConverterFactory(
					currentByPropertySetterConvererFactoryConfigurationData.NameMatcher,
					currentByPropertySetterConvererFactoryConfigurationData.BasePropertyGetterFactories,
					propertyGetterFactories => new CompilableTypeConverterByPropertySettingFactory(
						new CombinedCompilablePropertyGetterFactory(propertyGetterFactories),
						CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions.MatchAll,
						_allPropertiesToIgnoreToPropertySetterConversions,
						_nullSourceBehaviour
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
	}
}
