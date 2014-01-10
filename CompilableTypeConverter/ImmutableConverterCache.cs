using System;
using System.Collections.Generic;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverter
{
	public class ImmutableConverterCache
	{
		private readonly Dictionary<ConversionKey, object> _converterCache;
		public ImmutableConverterCache() : this(new Dictionary<ConversionKey, object>()) { }
		private ImmutableConverterCache(Dictionary<ConversionKey, object> converterCacheToStore)
		{
			if (converterCacheToStore == null)
				throw new ArgumentNullException("converterCacheToStore");

			_converterCache = converterCacheToStore;
		}

		public CacheEntry<TSource, TDest> TryToGet<TSource, TDest>()
		{
			object cachedConverter;
			if (!_converterCache.TryGetValue(new ConversionKey(typeof(TSource), typeof(TDest)), out cachedConverter))
				return null;

			return (CacheEntry<TSource, TDest>)cachedConverter;
		}

		/// <summary>
		/// The converter may be null to cache the fact that it is not possible to generate a converter for the TSource, TDest conversion
		/// </summary>
		public ImmutableConverterCache AddOrReplace<TSource, TDest>(ICompilableTypeConverter<TSource, TDest> converter)
		{
			var cacheEntry = (converter == null)
				? CacheEntry<TSource, TDest>.ConverterNotAvailable()
				: CacheEntry<TSource, TDest>.ConverterAvailable(converter);

			var converterCacheClone = new Dictionary<ConversionKey, object>(_converterCache);
			converterCacheClone[new ConversionKey(typeof(TSource), typeof(TDest))] = cacheEntry;
			return new ImmutableConverterCache(converterCacheClone);
		}

		public class CacheEntry<TSource, TDest>
		{
			public static CacheEntry<TSource, TDest> ConverterAvailable(ICompilableTypeConverter<TSource, TDest> converter)
			{
				if (converter == null)
					throw new ArgumentNullException("converter");
				return new CacheEntry<TSource, TDest>(true, converter);
			}
			public static CacheEntry<TSource, TDest> ConverterNotAvailable()
			{
				return new CacheEntry<TSource, TDest>(false, null);
			}
			private CacheEntry(bool isConverterAvailable, ICompilableTypeConverter<TSource, TDest> converter)
			{
				if (isConverterAvailable && (converter == null))
					throw new ArgumentException("converterAvailable must not be null if isConverterAvailable is true");
				else if (!isConverterAvailable && (converter != null))
					throw new ArgumentException("converterAvailable must not be null if isConverterAvailable is false");

				IsConverterAvailable = isConverterAvailable;
				Converter = converter;
			}

			public bool IsConverterAvailable { get; private set; }

			/// <summary>
			/// This will be non-null if IsConverterAvailable is true and null if it is false
			/// </summary>
			public ICompilableTypeConverter<TSource, TDest> Converter { get; private set; }
		}

		private sealed class ConversionKey
		{
			private readonly int _hashCode;
			public ConversionKey(Type sourceType, Type destType)
			{
				if (sourceType == null)
					throw new ArgumentNullException("sourceType");
				if (destType == null)
					throw new ArgumentNullException("destType");

				SourceType = sourceType;
				DestType = destType;
				_hashCode = SourceType.GetHashCode() ^ DestType.GetHashCode();
			}

			/// <summary>
			/// This will never be null
			/// </summary>
			public Type SourceType { get; private set; }

			/// <summary>
			/// This will never be null
			/// </summary>
			public Type DestType { get; private set; }

			public override int GetHashCode()
			{
				return _hashCode;
			}

			public override bool Equals(object obj)
			{
				if (obj == null)
					throw new ArgumentNullException("obj");
				var objConversionKey = obj as ConversionKey;
				if (objConversionKey == null)
					return false;
				return (objConversionKey.SourceType == SourceType) && (objConversionKey.DestType == DestType);
			}
		}
	}
}
