using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters;
using CompilableTypeConverter.PropertyGetters.Factories;

namespace CompilableTypeConverter.AutoMapperIntegration.PropertyGetters.Factories
{
    /// <summary>
    /// This IPropertyGetterFactory implementation looks for properties on a source type whose name matches a specified value (in the context of an INameMatcher)
    /// and whose value can to translated to a specified type by using AutoMapper
    /// </summary>
    public class AutoMapperEnabledPropertyGetterFactory : IPropertyGetterFactory
    {
        private INameMatcher _nameMatcher;
        private IConfigurationProvider _mappingConfig;
        public AutoMapperEnabledPropertyGetterFactory(INameMatcher nameMatcher, IConfigurationProvider mappingConfig)
        {
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (mappingConfig == null)
                throw new ArgumentNullException("mappingConfig");

            _nameMatcher = nameMatcher;
            _mappingConfig = mappingConfig;
        }

        /// <summary>
        /// This will return null if unable to return an IPropertyGetter for the named property that will return a value as the requested type
        /// </summary>
        public IPropertyGetter TryToGet(Type srcType, string propertyName, Type destPropertyType)
        {
            if (propertyName == null)
                throw new ArgumentNullException("property");
            if (destPropertyType == null)
                throw new ArgumentNullException("destPropertyType");

            var property = getProperty(srcType, propertyName, _nameMatcher, destPropertyType);
            if (property == null)
                return null;

            return (IPropertyGetter)Activator.CreateInstance(
                typeof(AutoMapperEnabledPropertyGetter<,>).MakeGenericType(
                    srcType,
                    destPropertyType
                ),
                property,
                new MappingEngine(_mappingConfig)
            );
        }

        /// <summary>
        /// This will return null if unable to return a PropertyInfo instance for the named property that will return a value as the requested type
        /// </summary>
        private PropertyInfo getProperty(Type srcType, string name, INameMatcher nameMatcher, Type destPropertyType)
        {
            if (srcType == null)
                throw new ArgumentNullException("srcType");
            name = (name ?? "").Trim();
            if (name == "")
                throw new ArgumentException("Null/blank name specified");
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (destPropertyType == null)
                throw new ArgumentNullException("destPropertyType");

            return srcType.GetProperties().FirstOrDefault(p =>
                p.GetIndexParameters().Length == 0
                && nameMatcher.IsMatch(name, p.Name)
                && canMap(p.PropertyType, destPropertyType)
            );
        }

        private bool canMap(Type srcType, Type destType)
        {
            if (destType.IsAssignableFrom(srcType))
                return true;

            // This is based on code in MappingEngine's IMappingEngineRunner.Map method
            var context = new ResolutionContext(
                _mappingConfig.FindTypeMapFor(null, srcType, destType),
                null,
                srcType,
                destType
            );
            return _mappingConfig.GetMappers().Any(mapper => mapper.IsMatch(context));
        }
    }
}
