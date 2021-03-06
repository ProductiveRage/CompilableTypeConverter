﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProductiveRage.CompilableTypeConverter.Common;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Factories;

namespace ProductiveRage.CompilableTypeConverter.TypeConverters.Factories
{
	public class CompilableTypeConverterByPropertySettingFactory : ICompilableTypeConverterFactory
    {
        private readonly ICompilablePropertyGetterFactory _propertyGetterFactory;
		private readonly PropertySettingTypeOptions _propertySettingType;
		private readonly HashSet<PropertyInfo> _propertiesToIgnore;
		private readonly ByPropertySettingNullSourceBehaviourOptions _nullSourceBehaviour;
		private readonly IEnumerable<PropertyInfo> _initialisedFlagsIfTranslatingNullsToEmptyInstances;
		public CompilableTypeConverterByPropertySettingFactory(
            ICompilablePropertyGetterFactory propertyGetterFactory,
            PropertySettingTypeOptions propertySettingType,
			IEnumerable<PropertyInfo> propertiesToIgnore,
			ByPropertySettingNullSourceBehaviourOptions nullSourceBehaviour,
			IEnumerable<PropertyInfo> initialisedFlagsIfTranslatingNullsToEmptyInstances)
		{
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");
			if (!Enum.IsDefined(typeof(PropertySettingTypeOptions), propertySettingType))
				throw new ArgumentOutOfRangeException("propertySettingType");
			if (propertiesToIgnore == null)
				throw new ArgumentNullException("propertiesToIgnore");
			if (!Enum.IsDefined(typeof(ByPropertySettingNullSourceBehaviourOptions), nullSourceBehaviour))
				throw new ArgumentOutOfRangeException("nullSourceBehaviour");
			if (initialisedFlagsIfTranslatingNullsToEmptyInstances == null)
				throw new ArgumentNullException("initialisedFlagsIfTranslatingNullsToEmptyInstances");
			if ((nullSourceBehaviour != ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues) && initialisedFlagsIfTranslatingNullsToEmptyInstances.Any())
				throw new ArgumentException("initialisedFlagsIfTranslatingNullsToEmptyInstances must be empty if nullSourceBehaviour is not CreateEmptyInstanceWithDefaultPropertyValues");

			_propertyGetterFactory = propertyGetterFactory;
            _propertySettingType = propertySettingType;
			_propertiesToIgnore = new HashSet<PropertyInfo>(propertiesToIgnore);
			if (_propertiesToIgnore.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in propertiesToIgnore set");
			_nullSourceBehaviour = nullSourceBehaviour;
			_initialisedFlagsIfTranslatingNullsToEmptyInstances = initialisedFlagsIfTranslatingNullsToEmptyInstances.ToList().AsReadOnly();
			if (_initialisedFlagsIfTranslatingNullsToEmptyInstances.Any(p => p == null))
				throw new ArgumentException("Null reference encountered in initialisedFlagsIfTranslatingNullToEmptyInstance set");
			if (_initialisedFlagsIfTranslatingNullsToEmptyInstances.Any(p => (p.PropertyType != typeof(bool)) && (p.PropertyType != typeof(bool?))))
				throw new ArgumentException("Encountered invalid property in initialisedFlagsIfTranslatingNullToEmptyInstance set, PropertyType must be bool or nullable bool");
		}

        public enum PropertySettingTypeOptions
        {
            MatchAll,
            MatchAsManyAsPossible
        }

        /// <summary>
		/// This will throw an exception if no suitable constructors were retrieved, it will never return null. Cases where a converter may not be generated include
		/// that where no public parameterless constructor exists on the destination type. It also includes the case where the MatchAll propertySettingType was
		/// specified at instantiation and there is at least one publicly-settable, non-indexed property that could not be dealt with.
		/// </summary>
        public ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
            var constructor = typeof(TDest).GetConstructor(new Type[0]);
			if (constructor == null)
				throw new ByPropertyMappingFailureException(typeof(TSource), typeof(TDest), ByPropertyMappingFailureException.FailureReasonOptions.NoParameterLessConstructor, null);

            var propertyGetters = new List<ICompilablePropertyGetter>();
            var propertiesToSet = new List<PropertyInfo>();
			foreach (var property in typeof(TDest).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
			{
                // If there isn't a public, non-indexed setter then move on
                if ((property.GetSetMethod() == null) || (property.GetIndexParameters().Length > 0))
                    continue;

				// If this is a property to ignore then do just that
				if (_propertiesToIgnore.Any(p => p.MatchesProperty(property)))
					continue;

                // If we can't retrieve a property getter for the property then either give up (if MatchAll) or push on (if MatchAsManyAsPossible)
                var propertyGetter = _propertyGetterFactory.TryToGet(typeof(TSource), property.Name, property.PropertyType);
                if (propertyGetter == null)
                {
                    if (_propertySettingType == PropertySettingTypeOptions.MatchAll)
						throw new ByPropertyMappingFailureException(typeof(TSource), typeof(TDest), ByPropertyMappingFailureException.FailureReasonOptions.UnableToMapProperty, property);
                    else
                        continue;
                }

                // Otherwise, add this property to the list!
                propertyGetters.Add(propertyGetter);
                propertiesToSet.Add(property);
			}

            // Have to use Activator.CreateInstance as CompilableTypeConverterByPropertySetting requires that TDest implement "new()" which we check at run
            // time above but can't know at compile time
            return (ICompilableTypeConverter<TSource, TDest>)Activator.CreateInstance(
                typeof(CompilableTypeConverterByPropertySetting<,>).MakeGenericType(
                    typeof(TSource),
                    typeof(TDest)
                ),
                propertyGetters,
                propertiesToSet,
				_nullSourceBehaviour,
				(_nullSourceBehaviour == ByPropertySettingNullSourceBehaviourOptions.CreateEmptyInstanceWithDefaultPropertyValues)
					? _initialisedFlagsIfTranslatingNullsToEmptyInstances.Where(p => typeof(TDest).HasProperty(p))
					: new PropertyInfo[0]
            );
		}

		/// <summary>
		/// This will throw an exception if no suitable constructors were retrieved, it will never return null. Cases where a converter may not be generated include
		/// that where no public parameterless constructor exists on the destination type. It also includes the case where the MatchAll propertySettingType was
		/// specified at instantiation and there is at least one publicly-settable, non-indexed property that could not be dealt with.
		/// </summary>
		ITypeConverter<TSource, TDest> ITypeConverterFactory.Get<TSource, TDest>()
        {
            return Get<TSource, TDest>();
        }
    }
}
