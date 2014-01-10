using System;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Factories;
using CompilableTypeConverter.TypeConverters;
using CompilableTypeConverter.TypeConverters.Factories;

namespace CompilableTypeConverter
{
	/// <summary>
	/// This static class is a configuration of the compilable type converter, intended for common cases. The class is thread-safe through use of locks, it's
	/// recommended that successfully-retrieved converters are cached by the caller (using the TryToGetConverter method) rather than calling the Conver method
	/// frequently for performance, since this will avoid the locks.
	/// </summary>
	public static class Converter
	{
		private static ExtendableCompilableTypeConverterFactory _constructorBasedConverterFactory;
		private static ExtendableCompilableTypeConverterFactory _propertySetterBasedConverterFactory;
		private static ImmutableConverterCache _converterCache;
		private static object _lock;
		static Converter()
		{
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
				basePropertyGetterFactories
			);
			_converterCache = new ImmutableConverterCache();
			_lock = new object();
		}

		/// <summary>
        /// This will throw an exception if unable to generate the requested mapping. If successful, the returned converter
        /// factory will be able to convert instances of TSourceNew as well as IEnumerable / Lists of them. For most cases,
		/// this method is only necessary for sub types - to convert from one type to another where all of the properties
		/// are primitives, the Convert and TryToGetConverter methods may be called without any prior CreateMap calls. If
		/// nested types are in the source or destination types then CreateMap needs  to be called for them. Where there
		/// are deeply-nested types for conversion, CreateMap should be called from the deepest level and worked up to
		/// the top.
        /// </summary>
		public static void CreateMap<TSource, TDest>()
		{
			// This will clear the converter cache since any new mappings may open up the possibility for improved conversions
			// so any subsequent converter requests should try to perform the work to generate the converter again
			lock (_lock)
			{
				var newConstructorBasedConverter = _constructorBasedConverterFactory.TryToGenerateConverter<TSource, TDest>();
				if (newConstructorBasedConverter != null)
				{
					_constructorBasedConverterFactory = _constructorBasedConverterFactory.AddNewConverter(newConstructorBasedConverter);
					_propertySetterBasedConverterFactory = _propertySetterBasedConverterFactory.AddNewConverter(newConstructorBasedConverter);
				}
				else
				{
					var newPropertySetterBasedConverter = _propertySetterBasedConverterFactory.TryToGenerateConverter<TSource, TDest>();
					if (newPropertySetterBasedConverter != null)
					{
						_constructorBasedConverterFactory = _constructorBasedConverterFactory.AddNewConverter(newPropertySetterBasedConverter);
						_propertySetterBasedConverterFactory = _propertySetterBasedConverterFactory.AddNewConverter(newPropertySetterBasedConverter);
					}
					else
						throw new Exception("Unable to create mapping");
				}
				_converterCache = new ImmutableConverterCache();
			}
		}

		/// <summary>
		/// Create a new target type instance from a source value - this will throw an exception if conversion fails
		/// </summary>
		public static TDest Convert<TSource, TDest>(TSource source)
		{
			var converter = TryToGetConverter<TSource, TDest>();
			if (converter == null)
				throw new ArgumentException("Unable to perform this mapping");
			
			return converter.Convert(source);
		}

		/// <summary>
		/// This will return null if a converter could not be generated
		/// </summary>
		public static ICompilableTypeConverter<TSource, TDest> TryToGetConverter<TSource, TDest>()
		{
			lock (_lock)
			{
				var cachedResult = _converterCache.TryToGet<TSource, TDest>();
				if (cachedResult != null)
					return cachedResult.Converter;

				var converter = _constructorBasedConverterFactory.Get<TSource, TDest>() ?? _propertySetterBasedConverterFactory.Get<TSource, TDest>();
				_converterCache = _converterCache.AddOrReplace<TSource, TDest>(converter);
				return converter;
			}
		}
	}
}
