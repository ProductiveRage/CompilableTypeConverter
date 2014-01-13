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
		/// factory will be able to convert instances of TSource as well as IEnumerable / Lists of them. For most cases,
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
			Exception mappingException;
			lock (_lock)
			{
				try
				{
					var converter = GenerateConverter<TSource, TDest>();
					_constructorBasedConverterFactory = _constructorBasedConverterFactory.AddNewConverter(converter);
					_propertySetterBasedConverterFactory = _propertySetterBasedConverterFactory.AddNewConverter(converter);
					_converterCache = new ImmutableConverterCache();
					return;
				}
				catch (Exception e)
				{
					mappingException = e;
				}
			}
			throw mappingException;
		}

		/// <summary>
		/// Create a new target type instance from a source value - this will throw an exception if conversion fails
		/// </summary>
		public static TDest Convert<TSource, TDest>(TSource source)
		{
			return GetConverter<TSource, TDest>().Convert(source);
		}

		/// <summary>
		/// This will throw an exception if unable to generate a converter for request TSource and TDest pair, it will never return null
		/// </summary>
		public static ICompilableTypeConverter<TSource, TDest> GetConverter<TSource, TDest>()
		{
			ICompilableTypeConverter<TSource, TDest> converter;
			Exception mappingException;
			lock (_lock)
			{
				var cachedResult = _converterCache.TryToGet<TSource, TDest>();
				if (cachedResult != null)
					return cachedResult.Converter;

				try
				{
					converter = GenerateConverter<TSource, TDest>();
					_converterCache = _converterCache.AddOrReplace<TSource, TDest>(converter);
					return converter;
				}
				catch (Exception e)
				{
					mappingException = e;
				}
			}
			throw mappingException;
		}

		/// <summary>
		/// This will throw an exception if unable to generate the requested converter
		/// </summary>
		private static ICompilableTypeConverter<TSource, TDest> GenerateConverter<TSource, TDest>()
		{
			// Attempt to generate a converter using the _constructorBasedConverterFactory and then the _propertySetterBasedConverterFactory.
			// If both fail then it's difficult to know for sure which exception is most useful to allow through. If the destination type has
			// a parameter-less constructor then allow the by-property-setter exception to be raised (if there was no parameter-less constructor
			// then the by-property-setter factory wouldn't have been able to do anything and we'll assume that the by-constructor factory
			// exception is more useful).
			Exception mappingException;
			try
			{
				return _constructorBasedConverterFactory.Get<TSource, TDest>();
			}
			catch (Exception byConstructorException)
			{
				try
				{
					return _propertySetterBasedConverterFactory.Get<TSource, TDest>();
				}
				catch (Exception byPropertySettingException)
				{
					if (typeof(TDest).GetConstructor(new Type[0]) != null)
						mappingException = byPropertySettingException;
					else
						mappingException = byConstructorException;
				}
			}
			throw mappingException;
		}
	}
}
