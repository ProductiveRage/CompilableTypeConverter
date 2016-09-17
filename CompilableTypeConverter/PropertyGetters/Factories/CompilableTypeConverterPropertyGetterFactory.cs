using System;
using System.Linq;
using ProductiveRage.CompilableTypeConverter.NameMatchers;
using ProductiveRage.CompilableTypeConverter.PropertyGetters.Compilable;
using ProductiveRage.CompilableTypeConverter.TypeConverters;

namespace ProductiveRage.CompilableTypeConverter.PropertyGetters.Factories
{
	/// <summary>
	/// This ICompilablePropertyGetterFactory implementation looks for properties on a source type whose name matches a specified value (in the context of
	/// an INameMatcher) and whose value can be retrieved to a specified type using a particular compilable type converter (so the type of the source
	/// property will have to match the source type of the type converter)
	/// </summary>
	public class CompilableTypeConverterPropertyGetterFactory<TPropertyOnSource, TPropertyAsRetrieved> : ICompilablePropertyGetterFactory
    {
        private INameMatcher _nameMatcher;
        private ICompilableTypeConverter<TPropertyOnSource, TPropertyAsRetrieved> _typeConverter;
        public CompilableTypeConverterPropertyGetterFactory(INameMatcher nameMatcher, ICompilableTypeConverter<TPropertyOnSource, TPropertyAsRetrieved> typeConverter)
        {
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (typeConverter == null)
                throw new ArgumentNullException("typeConverter");

            _nameMatcher = nameMatcher;
            _typeConverter = typeConverter;
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

            // If destination type does not match type converter's destination type then can not handle the request; return null
            if (destPropertyType != typeof(TPropertyAsRetrieved))
                return null;

            // Try to get a property we CAN retrieve and convert as requested..
            var property = srcType.GetProperties().FirstOrDefault(p =>
                p.GetIndexParameters().Length == 0
                && _nameMatcher.IsMatch(propertyName, p.Name)
                && p.PropertyType == typeof(TPropertyOnSource)
            );
            if (property == null)
                return null;

            // .. if successful, use to instantiate a CompilableTypeConverterPropertyGetter
            return (ICompilablePropertyGetter)Activator.CreateInstance(
                typeof(CompilableTypeConverterPropertyGetter<,,>).MakeGenericType(
                    srcType,
                    property.PropertyType,
                    destPropertyType
                ),
                property,
                _typeConverter
            );
        }

        IPropertyGetter IPropertyGetterFactory.TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            return TryToGet(srcType, propertyName, destPropertyType);
        }
    }
}
