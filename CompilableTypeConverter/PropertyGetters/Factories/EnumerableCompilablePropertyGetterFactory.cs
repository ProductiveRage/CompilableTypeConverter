﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProductiveRage.CompilableTypeConverter.NameMatchers;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable;
using ProductiveRage.CompilableTypeConverter.TypeConverters;

namespace ProductiveRage.CompilableTypeConverter.PropertyGetters.Factories
{
	/// <summary>
	/// This ICompilablePropertyGetterFactory implementation looks for properties on a source type whose name matches a specified value (in the context of
	/// an INameMatcher) and whose value can be retrieved as IEnumerable content with the its element type being passed through a particular compilable
	/// type converter (so the element type of the source property IEnumerable will have to match the source type of the type converter and the
	/// destination type will have to be an IEnumerable of the type converter's destination type)
	/// </summary>
	public class EnumerableCompilablePropertyGetterFactory<TPropertyOnSourceElement, TPropertyAsRetrievedElement> : ICompilablePropertyGetterFactory
    {
        private readonly INameMatcher _nameMatcher;
        private readonly ICompilableTypeConverter<TPropertyOnSourceElement, TPropertyAsRetrievedElement> _typeConverter;
		private readonly EnumerableSetNullHandlingOptions _enumerableSetNullHandling;
        public EnumerableCompilablePropertyGetterFactory(
			INameMatcher nameMatcher,
			ICompilableTypeConverter<TPropertyOnSourceElement, TPropertyAsRetrievedElement> typeConverter,
			EnumerableSetNullHandlingOptions enumerableSetNullHandling)
		{
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");
			if (!Enum.IsDefined(typeof(EnumerableSetNullHandlingOptions), enumerableSetNullHandling))
				throw new ArgumentOutOfRangeException("enumerableSetNullHandling");

            _nameMatcher = nameMatcher;
            _typeConverter = typeConverter;
			_enumerableSetNullHandling = enumerableSetNullHandling;
        }

        /// <summary>
        /// This will return null if unable to return an ICompilablePropertyGetter for the named property that will return a value as the requested type
        /// </summary>
        public ICompilablePropertyGetter TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            if (srcType == null)
                throw new ArgumentNullException("srcType");
            propertyName = (propertyName ?? "").Trim();
            if (propertyName == "")
                throw new ArgumentException("Null/empty propertyName specified");
            if (destPropertyType == null)
                throw new ArgumentNullException("destPropertyType");

            // Determine whether the destPropertyType implements IEnumerable<> and get the element type if so, if it doesn't match the destination
            // type of the converter then we'll not be able to work with it
            var destPropertyTypeAsEnumerableElement = tryToGetEnumerableElementTypeOf(destPropertyType);
            if (destPropertyTypeAsEnumerableElement != typeof(TPropertyAsRetrievedElement))
                return null;

            var possibleProperties = srcType.GetProperties().Where(p =>
                p.GetIndexParameters().Length == 0
                && _nameMatcher.IsMatch(propertyName, p.Name)
            );
            foreach (var property in possibleProperties)
            {
                // Try to get element type of srcType, if srcType implements IEnumerable<>
                var srcPropertyTypeAsEnumerableElement = tryToGetEnumerableElementTypeOf(property.PropertyType);
                if (srcPropertyTypeAsEnumerableElement == typeof(TPropertyOnSourceElement))
                {
                    return (ICompilablePropertyGetter)Activator.CreateInstance(
                        typeof(EnumerableCompilablePropertyGetter<,,,>).MakeGenericType(
                            srcType,
                            srcPropertyTypeAsEnumerableElement,
                            destPropertyTypeAsEnumerableElement,
                            destPropertyType
                        ),
                        property,
                        _typeConverter,
						_enumerableSetNullHandling
                    );
                }
            }
            return null;
        }

        IPropertyGetter IPropertyGetterFactory.TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            return TryToGet(srcType, propertyName, destPropertyType);
        }

        /// <summary>
        /// If the specified type has a single type argument and implements System.Collections.Generic.IEnumerable against that single type, return the type.
        /// Otherwise, return null. For example, if specified type is a System.Collections.Generic.List of strings then the string type will be returned.
        /// </summary>
        private Type tryToGetEnumerableElementTypeOf(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var genericArgs = type.GetGenericArguments();
            if ((genericArgs == null) || (genericArgs.Length != 1))
                return null;

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(genericArgs[0]);
            if (!enumerableType.IsAssignableFrom(type))
                return null;

            return genericArgs[0];
        }
    }
}
