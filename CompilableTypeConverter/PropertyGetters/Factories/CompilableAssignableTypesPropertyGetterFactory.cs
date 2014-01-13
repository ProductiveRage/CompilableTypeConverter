using System;
using System.Linq;
using System.Reflection;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Compilable;

namespace CompilableTypeConverter.PropertyGetters.Factories
{
    /// <summary>
    /// This ICompilablePropertyGetterFactory implementation looks for properties on a source type whose name matches a specified value (in the context of
    /// an INameMatcher) and whose value can to assigned directly to a specified type
    /// </summary>
    public class CompilableAssignableTypesPropertyGetterFactory : ICompilablePropertyGetterFactory
    {
        private INameMatcher _nameMatcher;
        public CompilableAssignableTypesPropertyGetterFactory(INameMatcher nameMatcher)
        {
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");

            _nameMatcher = nameMatcher;
        }

        /// <summary>
        /// This will return null if unable to return an ICompilablePropertyGetter for the named property that will return a value as the requested type
        /// </summary>
        public ICompilablePropertyGetter TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            if (propertyName == null)
                throw new ArgumentNullException("property");
            if (destPropertyType == null)
                throw new ArgumentNullException("destPropertyType");

            var property = getProperty(srcType, propertyName, destPropertyType);
            if (property == null)
                return null;

            return (ICompilablePropertyGetter)Activator.CreateInstance(
                typeof(CompilableAssignableTypesPropertyGetter<,>).MakeGenericType(
                    srcType,
                    destPropertyType
                ),
                property
            );
        }

        IPropertyGetter IPropertyGetterFactory.TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            return TryToGet(srcType, propertyName, destPropertyType);
        }

        /// <summary>
        /// This will return null if unable to return a PropertyInfo instance for the named property that will return a value as the requested type
        /// </summary>
        private PropertyInfo getProperty(Type srcType, string name, Type destPropertyType)
        {
            if (srcType == null)
                throw new ArgumentNullException("srcType");
            name = (name ?? "").Trim();
            if (name == "")
                throw new ArgumentException("Null/blank name specified");
            if (destPropertyType == null)
                throw new ArgumentNullException("destPropertyType");

            return srcType.GetProperties().FirstOrDefault(p =>
                p.GetIndexParameters().Length == 0
                && _nameMatcher.IsMatch(name, p.Name)
                && destPropertyType.IsAssignableFrom(p.PropertyType)
            );
        }
    }
}
