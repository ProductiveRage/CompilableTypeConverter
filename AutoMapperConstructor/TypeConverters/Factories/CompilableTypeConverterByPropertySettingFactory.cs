﻿using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapperConstructor.PropertyGetters.Compilable;
using AutoMapperConstructor.PropertyGetters.Factories;

namespace AutoMapperConstructor.TypeConverters.Factories
{
    public class CompilableTypeConverterByPropertySettingFactory : ICompilableTypeConverterFactory
    {
        private ICompilablePropertyGetterFactory _propertyGetterFactory;
        private PropertySettingTypeOptions _propertySettingType;
        public CompilableTypeConverterByPropertySettingFactory(
            ICompilablePropertyGetterFactory propertyGetterFactory,
            PropertySettingTypeOptions propertySettingType)
		{
            if (propertyGetterFactory == null)
                throw new ArgumentNullException("propertyGetterFactory");
            if (!Enum.IsDefined(typeof(PropertySettingTypeOptions), propertySettingType))
                throw new ArgumentOutOfRangeException("propertySettingType");

			_propertyGetterFactory = propertyGetterFactory;
            _propertySettingType = propertySettingType;
		}

        public enum PropertySettingTypeOptions
        {
            MatchAll,
            MatchAsManyAsPossible
        }

        /// <summary>
        /// This will return null if no suitable conversion could be prepared. This will be the case if there is no public parameterless constructor. It will also
        /// be the case if the MatchAll propertySettingType was specified at instantiation and there is at least one publicly-settable, non-indexed property that
        /// could not be dealt with.
		/// </summary>
        public ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
            var constructor = typeof(TDest).GetConstructor(new Type[0]);
            if (constructor == null)
                return null;

            var propertyGetters = new List<ICompilablePropertyGetter>();
            var propertiesToSet = new List<PropertyInfo>();
			foreach (var property in typeof(TDest).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
			{
                // If there isn't a public, non-indexed setter then move on
                if ((property.GetSetMethod() == null) || (property.GetIndexParameters().Length > 0))
                    continue;

                // If we can't retrieve a property getter for the property then either give up (if MatchAll) or push on (if MatchAsManyAsPossible)
                var propertyGetter = _propertyGetterFactory.Get(typeof(TSource), property.Name, property.PropertyType);
                if (propertyGetter == null)
                {
                    if (_propertySettingType == PropertySettingTypeOptions.MatchAll)
                        return null;
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
                propertiesToSet
            );
		}

        ITypeConverter<TSource, TDest> ITypeConverterFactory.Get<TSource, TDest>()
        {
            return Get<TSource, TDest>();
        }
    }
}