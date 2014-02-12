﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CompilableTypeConverter.ConverterWrapperHelpers;
using CompilableTypeConverter.TypeConverters;
using CompilableTypeConverter.PropertyGetters.Compilable;

namespace CompilableTypeConverter
{
	/// <summary>
	/// This static class is a further wrapper around the ConverterWrapper and is intended to make many of the most common use cases for the Type Converter to
	/// be very easy. It starts off configured with basic conversion capabilities and will automatically add new converters generated by successful calls to
	/// any of the Convert, CreateMap or GetConverter to its repertoire. Access to the methods is thread-safe through use of locks, it's recommended that
	/// successfully-retrieved converters are cached by the caller (using the TryToGetConverter method) rather than calling the Convert method frequently
	/// for performance, since this will avoid the locks (but this is not compulsory).
	/// </summary>
	public static class Converter
	{
		private static ConverterWrapper _converter;
		private static object _lock;
		static Converter()
		{
			_lock = new object();
			Reset();
		}

		/// <summary>
		/// This may be used to specify custom rules for the mapping to be created. It will never return null. When the Create
		/// method is called on the returned Configurer instance a new mapping will be created if possible (if not then an
		/// exception will be raised). As with CreateMap, Convert and GetNewConverter, if there has already been a converter
		/// generated for the specified TSource, TDest pair then this will be returned and no new converter will be created
		/// (so any custom mapping rules will end up being ignored).
		/// </summary>
		public static ConverterConfigurer<TSource, TDest> BeginCreateMap<TSource, TDest>()
		{
			return new ConverterConfigurer<TSource, TDest>(new PropertyInfo[0]);
		}

		/// <summary>
        /// This will throw an exception if unable to generate the requested mapping. If successful, the returned converter
		/// factory will be able to convert instances of TSource to TDest instances. It may also use this conversion knowledge
		/// when generating later conversions - eg. if CreateMap is subsequently called for TSource2 ant TDest2, where TSource
		/// has a property TSource that must be mapped to a property on TDest2 of type TDest, this new converter may be used
		/// there. If TSource2's property is of type IEnumerable TSource, and must be mapped to a property on TDest2 of type
		/// IEnumerable TDest, this will also be handled using this new converter. For most cases, this method is only necessary
		/// for such sub types - to convert from one type to another where all of the properties are primitives, the Convert and
		/// TryToGetConverter methods may be called without any prior CreateMap calls. If nested types are in the source or
		/// destination types then CreateMap needs to be called for them. Where there are deeply-nested types for conversion,
		/// CreateMap should be called from the deepest level and worked up to the top.
        /// </summary>
		public static void CreateMap<TSource, TDest>(
			IEnumerable<PropertyInfo> propertiesToIgnoreIfSettingPropertiesOnTDest,
			ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			// This is just a wrapper around GetConverter for when you don't immediately care about the generated
			// converter, you just need to build up some mappings
			GetConverter<TSource, TDest>(propertiesToIgnoreIfSettingPropertiesOnTDest, converterOverrideBehaviour);
		}
		public static void CreateMap<TSource, TDest>(
			ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			CreateMap<TSource, TDest>(new PropertyInfo[0], converterOverrideBehaviour);
		}

		/// <summary>
		/// Create a new target type instance from a source value - this will throw an exception if conversion fails
		/// </summary>
		public static TDest Convert<TSource, TDest>(
			TSource source,
			IEnumerable<PropertyInfo> propertiesToIgnoreIfSettingPropertiesOnTDest,
			ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			// This is also a wrapper around GetConverter to make it easy for callers to get going (for performance reasons,
			// it would be best to call GetConverter from the caller and store the converter reference somewhere since that
			// would avoid the lock around each request that the GetConverter method requires).
			return GetConverter<TSource, TDest>(propertiesToIgnoreIfSettingPropertiesOnTDest, converterOverrideBehaviour).Convert(source);
		}
		public static TDest Convert<TSource, TDest>(
			TSource source,
			ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			return Convert<TSource, TDest>(source, new PropertyInfo[0], converterOverrideBehaviour);
		}

		/// <summary>
		/// This will throw an exception if unable to generate a converter for request TSource and TDest pair, it will never return null
		/// </summary>
		public static ICompilableTypeConverter<TSource, TDest> GetConverter<TSource, TDest>(
			IEnumerable<PropertyInfo> propertiesToIgnoreIfSettingPropertiesOnTDest,
			ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			if (propertiesToIgnoreIfSettingPropertiesOnTDest == null)
				throw new ArgumentNullException("propertiesToIgnoreIfSettingPropertiesOnTDest");
			var propertiesToIgnoreList = propertiesToIgnoreIfSettingPropertiesOnTDest.ToList();
			if (propertiesToIgnoreList.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in propertiesToIgnoreIfSettingPropertiesOnTDest set ");
			if (!Enum.IsDefined(typeof(ConverterOverrideBehaviourOptions), converterOverrideBehaviour))
				throw new ArgumentOutOfRangeException("converterOverrideBehaviour");

			lock (_lock)
			{
				// Note: new PropertyInfo[0] is always passed for initialisedFlagsIfTranslatingNullsToEmptyInstances for since this
				// wrapper always specifies ByPropertySettingNullSourceBehaviourOptions.UseDestDefaultIfSourceIsNull and the
				// initialised-instance flags are only applicable to the CreateEmptyInstanceWithDefaultPropertyValues
				// ByPropertySettingNullSourceBehaviourOptions configuration
				return _converter.GetConverter<TSource, TDest>(
					propertiesToIgnoreIfSettingPropertiesOnTDest,
					new PropertyInfo[0], // ByPropertySettingNullSourceBehaviourOptions.UseDestDefaultIfSourceIsNull
					converterOverrideBehaviour
				);
			}
		}
		public static ICompilableTypeConverter<TSource, TDest> GetConverter<TSource, TDest>(
			ConverterOverrideBehaviourOptions converterOverrideBehaviour = ConverterOverrideBehaviourOptions.UseAnyExistingConverter)
		{
			return GetConverter<TSource, TDest>(new PropertyInfo[0], converterOverrideBehaviour);
		}

		/// <summary>
		/// This will reset entirely to the base state, as if no calls to Convert, Create or GetConverter had been made
		/// </summary>
		public static void Reset()
		{
			lock (_lock)
			{
				_converter = new ConverterWrapper(
					ByPropertySettingNullSourceBehaviourOptions.UseDestDefaultIfSourceIsNull,
					EnumerableSetNullHandlingOptions.ReturnNullSetForNullInput
				);
			}
		}
	}
}
