﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CompilableTypeConverter.ConverterWrapperHelpers;
using CompilableTypeConverter.PropertyGetters.Compilable;
using CompilableTypeConverter.QueryableExtensions.ProjectionConverterHelpers;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverter.QueryableExtensions
{
	/// <summary>
	/// This is a variation on the static Converter class and is intended to make easy many of the common projections that may occur in IQueryable data sets.
	/// Entity Framework is targeted specified but it should work with many other IQueryable sources. There are limitations to Entity Framework projections
	/// that this addresses, such as not being able to use constructors with parameters in the destination type - the Converter class could be used if the
	/// IQueryable data has AsEnumerable called on it, but this may result in more data being retrieved than necessary. The benefit of projections is that
	/// they are translated into expressions that the data store (such as SQL) can process and this can result in queries being generated that retrieve only
	/// the required data for the destination type. As with the Converter, it starts off configured with basic conversion capabilities and will automatically
	/// add new converters generated by successful calls to CreateMap to its repertoire. Access to the methods is thread-safe through use of locks (the
	/// projection functions returned from the GetProjection method may be cached for performance - to avoid the locking - but this performance benefit
	/// may be negligible in comparison to the cost of a db bit).
	/// </summary>
	public static class ProjectionConverter
	{
		private static ConverterWrapper _converter;
		private readonly static Dictionary<Tuple<Type, Type>, ProjectionCacheEntry> _projectionDataTypeCache;
		private readonly static Dictionary<AnonymousTypePropertyInfoSet, InterimTypeWithIsInitialisedFlag> _interimTypeCache;
		private readonly static object _lock;
		static ProjectionConverter()
		{
			_lock = new object();
			_projectionDataTypeCache = new Dictionary<Tuple<Type, Type>, ProjectionCacheEntry>();
			_interimTypeCache = new Dictionary<AnonymousTypePropertyInfoSet, InterimTypeWithIsInitialisedFlag>();
			Reset();
		}

		/// <summary>
		/// This is used to prepare mappings from IQueryable sets TSource to IEnumerable sets of TDest, or for any properties of nested types that must be
		/// translated in order to convert IQueryable sets. If nested types are in the source or destination types then CreateMap needs to be called for them
		/// first, where there are deeply-nested types for conversion, CreateMap should be called from the deepest level and worked up to the top. This will
		/// throw an exception if unable to generate the requested mapping.
		/// </summary>
		public static void CreateMap<TSource, TDest>()
		{
			lock (_lock)
			{
				var lookupKey = Tuple.Create(typeof(TSource), typeof(TDest));
				if (_projectionDataTypeCache.ContainsKey(lookupKey))
					return;

				// Generate a converter that would go straight from TSource to TDest
				var sourceToDestConverter = _converter.GetConverter<TSource, TDest>(
					new PropertyInfo[0], // propertiesToIgnoreIfSettingPropertiesOnTDest
					new PropertyInfo[0], // initialisedFlagsIfTranslatingNullsToEmptyInstances
					ConverterOverrideBehaviourOptions.UseAnyExistingConverter
				);

				// Generate a type that has only the properties from TSource that would be mapped to TDest
				var interimTypeWithIsInitialisedFlag = GetInterimTypeWithinAcquiredConverterCacheLock(sourceToDestConverter.PropertyMappings);

				// Generate the projection data required - this involves a call to CreateProjectionDataWithinAcquiredConverterCacheLock with type params
				// TSource, TInterim and TDest (which requires reflection since we only have a reference to TInterim now - since it's potentially only
				// just been created - rather than a generic type param)
				var createProjectionDataWithinAcquiredConverterCacheLockMethod = typeof(ProjectionConverter)
					.GetMethod("CreateProjectionDataWithinAcquiredConverterCacheLock", BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod(typeof(TSource), interimTypeWithIsInitialisedFlag.InterimType, typeof(TDest));
				_projectionDataTypeCache.Add(
					lookupKey,
					(ProjectionCacheEntry)createProjectionDataWithinAcquiredConverterCacheLockMethod.Invoke(
						null,
						new object[]
						{
							sourceToDestConverter,
							interimTypeWithIsInitialisedFlag.IsInitialisedFlag
						}
					)
				);
			}
		}

		private static ProjectionCacheEntry CreateProjectionDataWithinAcquiredConverterCacheLock<TSource, TInterim, TDest>(
			ICompilableTypeConverter<TSource, TDest> sourceToDestConverter,
			PropertyInfo interimTypeIsInitialisedFlag)
		{
			if (sourceToDestConverter == null)
				throw new ArgumentNullException("sourceToDestConverter");
			if (interimTypeIsInitialisedFlag == null)
				throw new ArgumentNullException("interimTypeIsInitialisedFlag");
			if (interimTypeIsInitialisedFlag.DeclaringType != typeof(TInterim))
				throw new ArgumentException("interimTypeIsInitialisedFlag's DeclaringType must be TInterim");

			// Generate a source:interim converter, if the source value is null then an interim type with false is-initialised flag will be generated,
			// otherwise a fully-populate interim instance with is-initialised flag set to true will result (this is because the _converter instance
			// is configured with the CreateEmptyInstanceWithDefaultPropertyValues option).
			var sourceToInterimConverter = _converter.GetConverter<TSource, TInterim>(
				new PropertyInfo[0], // propertiesToIgnoreIfSettingPropertiesOnTDest,
				new[] { interimTypeIsInitialisedFlag }, // initialisedFlagsIfTranslatingNullsToEmptyInstances,
				ConverterOverrideBehaviourOptions.UseAnyExistingConverter
			);

			// Generate an interim:dest converter. This is done in two steps, first a basic interim:dest converter is initialised using the _converter,
			// then this needs wrapping in an UninitialisedSourceToNullHandlingConverter which re-inserts the logic around the interim type's is-
			// initialised flag; if the input is an interim type instance with is-initialised flag set to false, then the result should be null
			var interimToDestConverter = new UninitialisedSourceToNullHandlingConverter<TInterim, TDest>(
				_converter.GetConverter<TInterim, TDest>(
					new PropertyInfo[0], // propertiesToIgnoreIfSettingPropertiesOnTDest,
					new PropertyInfo[0], // initialisedFlagsIfTranslatingNullsToEmptyInstances,
					ConverterOverrideBehaviourOptions.UseAnyExistingConverter
				),
				interimTypeIsInitialisedFlag
			);

			// This interim:dest converter should override in the wrapped "_converter" that the call to GetConverter above will have generated. This will
			// be important if there are further translations generated where there are properties that required this interim:dest mapping (since, if we
			// don't make this call, then the translation that the wrapped _converter will use won't know to set the is-initialised flag correctly).
			_converter.SetConverter<TInterim, TDest>(interimToDestConverter);
			
			return new ProjectionCacheEntry(
				typeof(TSource),
				typeof(TDest),
				typeof(TInterim),
				sourceToInterimConverter,
				interimToDestConverter
			);
		}

		/// <summary>
		/// This will return a Func which will translate from an IQueryable set of TSource to an IEnumerable set of TDest, via a projection expression which
		/// will ensure that the minimum data for the translation be retrieved from the source data (the standard Converter static class could be used if
		/// the IQueryable set could have AsEnumerable called on it first, but this might result in more data being retrieved than necessary - this
		/// approach should mean that only properties on the source types that are populated are those that are required to generate the destination
		/// data set).
		/// </summary>
		public static Func<IQueryable<TSource>, IEnumerable<TDest>> GetProjection<TSource, TDest>()
		{
			lock (_lock)
			{
				ProjectionCacheEntry projectionData;
				if (_projectionDataTypeCache.TryGetValue(Tuple.Create(typeof(TSource), typeof(TDest)), out projectionData))
					return projectionData.GetProjection<TSource, TDest>();
			}
			throw new ArgumentException(string.Format(
				"No conversion data is available for this mapping, ensure that CreateMap has been called for this TSource, TDest pair ({0}, {1}) before calling this method",
				typeof(TSource).Name,
				typeof(TDest).Name
			));
		}

		/// <summary>
		/// This will reset entirely to the base state, as if no calls to Convert, Create or GetConverter had been made
		/// </summary>
		public static void Reset()
		{
			lock (_lock)
			{
				// The CreateEmptyInstanceWithDefaultPropertyValues and AssumeNonNullInput configuration options are to deal with generating project
				// expressions for Entity Framework IQueryable sets. The first is because it's not allowable to look at an input and generate a null
				// reference from it (an "Unable to create a null constant value of type '{whatever}'. Only entity types, enumeration types or primitive
				// types are supported in this context" will will be thrown). Instead we generate an interim type which is never null but which has an
				// is-initialised flag, this will be set to false if the source value should result in a null destination instance. The AssumeNonNullInput
				// option for EnumerableSetNullHandlingOptions is required for a similar reason - if an input is an enumerable set then it may not be
				// translated to null in some cases (like if the input set is null) and to a non-null list in other cases (if it's not null) (another
				// NotSupportedException will be thrown with message "Unable to create a null constant value of type '{whatever}'. Only entity types,
				// enumeration types or primitive types are supported in this context").
				_converter = new ConverterWrapper(
					ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues,
					EnumerableSetNullHandlingOptions.AssumeNonNullInput
				);
				_projectionDataTypeCache.Clear();

				// Note: There's no point resetting the interimTypeCache since the cached results don't change based upon any configuration, the same
				// type should always be used when there same set of properties are required on it.
			}
		}

        /// <summary>
		/// A lock on the projectionDataTypeCache must be acquired before calling this method since it will be accessed inside
        /// </summary>
		private static InterimTypeWithIsInitialisedFlag GetInterimTypeWithinAcquiredConverterCacheLock(IEnumerable<PropertyMappingDetails> converterMappingDetails)
        {
			if (converterMappingDetails == null)
				throw new ArgumentNullException("converterMappingDetails");
				
			// Determine what properties should be on the interim type. If the source and destination types are ones for which a converter has already
			// been configured (meaning it will be available in the cache) the the interim type's property type should be the interim type relating to
			// that conversion. The same thing is required if the source and destination types implement IEnumerable<> and there is a converter
			// configured from the element type of the source type enumerable to the element type of the destination (in this case, the interim
			// property type should be an IEnumerable<> of the interim type used with that conversion), this is because the ConverterWrapper
			// will automatically deal with conversions of generic IEnumerables if it can map from the source element type to the destination
			// element. If neither of these lookup approaches succeed, then the interim type's property type should match the source type's
			// (presumably no complex conversion is required; it might be assignable-to conversion or an enum conversion).
			var interimTypeProperties = new List<PropertyInfo>();
			foreach (var mappedProperty in converterMappingDetails)
			{
				if (mappedProperty == null)
					throw new ArgumentException("Null reference encountered in converterMappingDetails set");

				var fromType = mappedProperty.SourceProperty.PropertyType;
				var toType = mappedProperty.DestinationType;
				ProjectionCacheEntry projectionData;
				if (_projectionDataTypeCache.TryGetValue(Tuple.Create(fromType, toType), out projectionData))
				{
					interimTypeProperties.Add(
						new PlaceHolderNonIndexedPropertyInfo(
							mappedProperty.SourceProperty.Name,
							projectionData.InterimType
						)
					);
					continue;
				}

				var fromEnumerableElementTypes = GetElementTypesOfAnyImplementedGenericEnumerables(fromType).ToArray();
				var toEnumerableElementTypes = GetElementTypesOfAnyImplementedGenericEnumerables(toType).ToArray();
				projectionData = null;
				for (var indexFrom = 0; indexFrom < fromEnumerableElementTypes.Length; indexFrom++)
				{
					for (var indexTo = 0; indexTo < toEnumerableElementTypes.Length; indexTo++)
					{
						var enumerableTypeConversionLookup = Tuple.Create(
							fromEnumerableElementTypes[indexFrom],
							toEnumerableElementTypes[indexFrom]
						);
						if (_projectionDataTypeCache.TryGetValue(enumerableTypeConversionLookup, out projectionData))
							break;
					}
					if (projectionData != null)
						break;
				}
				if (projectionData != null)
				{
					interimTypeProperties.Add(
						new PlaceHolderNonIndexedPropertyInfo(
							mappedProperty.SourceProperty.Name,
							typeof(IEnumerable<>).MakeGenericType(projectionData.InterimType)
						)
					);
					continue;
				}

				interimTypeProperties.Add(mappedProperty.SourceProperty);
			}

			var interimTypePropertyData = new AnonymousTypePropertyInfoSet(interimTypeProperties);
			lock (_interimTypeCache)
            {
				InterimTypeWithIsInitialisedFlag interimTypeWithIsInitialisedFlag;
				if (_interimTypeCache.TryGetValue(interimTypePropertyData, out interimTypeWithIsInitialisedFlag))
					return interimTypeWithIsInitialisedFlag;

				PropertyInfo interimTypeIsInitialisedFlag = null;
				while (true)
				{
					interimTypeIsInitialisedFlag = new PlaceHolderNonIndexedPropertyInfo(
						"<>Initialised-" + Guid.NewGuid(),
						typeof(bool)
					);
					if (!interimTypeProperties.Any(p => p.Name == interimTypeIsInitialisedFlag.Name))
						break;
				}

				// Generate a new type with the required properties plus the is-initialised flag
				var interimType = AnonymousTypeCreator.DefaultInstance.Get(
					new AnonymousTypePropertyInfoSet(interimTypeProperties.Concat(new[] { interimTypeIsInitialisedFlag }))
				);

				// Now get a real PropertyInfo reference for the is-initialised flag (in case anything needs to access it later on that
				// requires real data rather than the placeholder version used above)
				interimTypeIsInitialisedFlag = interimType.GetProperty(interimTypeIsInitialisedFlag.Name);

				// Now we have all of the information to populate the cache and return the data
				interimTypeWithIsInitialisedFlag = new InterimTypeWithIsInitialisedFlag(
					interimType,
					interimTypeIsInitialisedFlag
				);
				_interimTypeCache[interimTypePropertyData] = interimTypeWithIsInitialisedFlag;
				return interimTypeWithIsInitialisedFlag;
            }
        }

		private static IEnumerable<Type> GetElementTypesOfAnyImplementedGenericEnumerables(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			IEnumerable<Type> interfaces = type.GetInterfaces();
			if (type.IsInterface)
				interfaces = interfaces.Concat(new[] { type });
			return interfaces
				.Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
				.Select(i => i.GetGenericArguments()[0]);
		}

		private class InterimTypeWithIsInitialisedFlag
		{
			public InterimTypeWithIsInitialisedFlag(Type interimType, PropertyInfo isInitialisedFlag)
			{
				if (interimType == null)
					throw new ArgumentNullException("interimType");
				if (isInitialisedFlag == null)
					throw new ArgumentNullException("isInitialisedFlag");
				if (isInitialisedFlag.PropertyType != typeof(bool))
					throw new ArgumentException("interimTypeIsInitialisedFlag's PropertyType must be bool");
				if (isInitialisedFlag.DeclaringType != interimType)
					throw new ArgumentException("isInitialisedFlag's DeclaringType must match interimType");

				InterimType = interimType;
				IsInitialisedFlag = isInitialisedFlag;
			}

			/// <summary>
			/// This will never be null
			/// </summary>
			public Type InterimType { get; private set; }

			/// <summary>
			/// This will never be null, it will always be a bool property of InterimType
			/// </summary>
			public PropertyInfo IsInitialisedFlag { get; private set; }
		}

		private class ProjectionCacheEntry
		{
			private readonly object _projection;
			public ProjectionCacheEntry(Type sourceType, Type destType, Type interimType, object sourceToInterimConverter, object interimToDestConverter)
			{
				if (sourceType == null)
					throw new ArgumentNullException("sourceType");
				if (destType == null)
					throw new ArgumentNullException("destType");
				if (interimType == null)
					throw new ArgumentNullException("interimType");
				if (sourceToInterimConverter == null)
					throw new ArgumentNullException("sourceToInterimConverter");
				if (interimToDestConverter == null)
					throw new ArgumentNullException("interimToDestConverter");
				if (!typeof(ICompilableTypeConverter<,>).MakeGenericType(sourceType, interimType).IsAssignableFrom(sourceToInterimConverter.GetType()))
					throw new ArgumentException("sourceToInterimConverter must be an ICompilableTypeConverter mapping sourceType to interimType");
				if (!typeof(ICompilableTypeConverter<,>).MakeGenericType(interimType, destType).IsAssignableFrom(interimToDestConverter.GetType()))
					throw new ArgumentException("interimToDestConverter must be an ICompilableTypeConverter mapping interimType to destType");

				SourceType = sourceType;
				InterimType = interimType;
				DestType = destType;

				_projection = 
					typeof(Projection<,,>).MakeGenericType(
						sourceType,
						interimType,
						destType
					)
					.GetConstructor(new[] {
						typeof(ICompilableTypeConverter<,>).MakeGenericType(sourceType, interimType),
						typeof(ICompilableTypeConverter<,>).MakeGenericType(interimType, destType),
					})
					.Invoke(new[] { sourceToInterimConverter, interimToDestConverter });
			}

			/// <summary>
			/// This will never be null
			/// </summary>
			public Type SourceType { get; private set; }

			/// <summary>
			/// This will never be null
			/// </summary>
			public Type InterimType { get; private set; }

			/// <summary>
			/// This will never be null
			/// </summary>
			public Type DestType { get; private set; }

			public Func<IQueryable<TSource>, IEnumerable<TDest>> GetProjection<TSource, TDest>()
			{
				if (typeof(TSource) != SourceType)
					throw new ArgumentException("typeof(TSource) must match SourceType");
				if (typeof(TDest) != DestType)
					throw new ArgumentException("typeof(TDest) must match DestType");

				return ((IProjection<TSource, TDest>)_projection).GetProjection();
			}

			private interface IProjection<TSource, TDest>
			{
				/// <summary>
				/// This must never return null
				/// </summary>
				Func<IQueryable<TSource>, IEnumerable<TDest>> GetProjection();
			}

			private class Projection<TSource, TInterim, TDest> : IProjection<TSource, TDest>
			{
				private readonly ICompilableTypeConverter<TSource, TInterim> _sourceTypeToInterimTypeConversion;
				private readonly ICompilableTypeConverter<TInterim, TDest> _interimTypeToDestTypeConversion;
				public Projection(
					ICompilableTypeConverter<TSource, TInterim> sourceTypeToInterimTypeConversion,
					ICompilableTypeConverter<TInterim, TDest> interimTypeToDestTypeConversion)
				{
					if (sourceTypeToInterimTypeConversion == null)
						throw new ArgumentNullException("sourceTypeToInterimTypeConversion");
					if (interimTypeToDestTypeConversion == null)
						throw new ArgumentNullException("interimTypeToDestTypeConversion");

					_sourceTypeToInterimTypeConversion = sourceTypeToInterimTypeConversion;
					_interimTypeToDestTypeConversion = interimTypeToDestTypeConversion;
				}

				/// <summary>
				/// This will never return null
				/// </summary>
				public Func<IQueryable<TSource>, IEnumerable<TDest>> GetProjection()
				{
					return source => source
						.Select(_sourceTypeToInterimTypeConversion.GetTypeConverterFuncExpression())
						.AsEnumerable()
						.Select(_interimTypeToDestTypeConversion.Convert);
				}
			}
		}
	}
}