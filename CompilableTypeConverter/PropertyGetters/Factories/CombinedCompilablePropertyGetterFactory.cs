﻿using System;
using System.Collections.Generic;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable;

namespace ProductiveRage.CompilableTypeConverter.PropertyGetters.Factories
{
	/// <summary>
	/// This considers multiple ICompilablePropertyGetterFactory implementations when trying to find a match - each will be tried in turn when the Get method is called
	/// until one of them can return a non-null value. If all fail then Get will still return null.
	/// </summary>
	public class CombinedCompilablePropertyGetterFactory : ICompilablePropertyGetterFactory
    {
        private List<ICompilablePropertyGetterFactory> _propertyGetterFactories;
        public CombinedCompilablePropertyGetterFactory(IEnumerable<ICompilablePropertyGetterFactory> propertyGetterFactories)
        {
            if (propertyGetterFactories == null)
                throw new ArgumentNullException("nameMatcher");

            var propertyGetterFactoriesList = new List<ICompilablePropertyGetterFactory>();
            foreach (var propertyGetterFactory in propertyGetterFactories)
            {
                if (propertyGetterFactory == null)
                    throw new ArgumentException("Null entry encountered in propertyGetterFactories");
                propertyGetterFactoriesList.Add(propertyGetterFactory);
            }
            _propertyGetterFactories = propertyGetterFactoriesList;
        }

        /// <summary>
        /// This will return null if unable to return an ICompilablePropertyGetter for the named property that will return a value as the requested type
        /// </summary>
        public ICompilablePropertyGetter TryToGet(Type srcType, string propertyName, Type destType)
        {
            if (srcType == null)
                throw new ArgumentNullException("srcType");
            if (propertyName == null)
                throw new ArgumentNullException("property");
            if (destType == null)
                throw new ArgumentNullException("destType");

            foreach (var propertyGetterFactory in _propertyGetterFactories)
            {
                var propertyGetter = propertyGetterFactory.TryToGet(srcType, propertyName, destType);
                if (propertyGetter != null)
                    return propertyGetter;
            }
            return null;
        }

        IPropertyGetter IPropertyGetterFactory.TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            return TryToGet(srcType, propertyName, destPropertyType);
        }
    }
}
