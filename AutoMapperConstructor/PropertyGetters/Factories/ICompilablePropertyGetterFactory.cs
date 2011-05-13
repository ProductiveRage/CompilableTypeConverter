using System;

namespace AutoMapperConstructor.PropertyGetters.Factories
{
    public interface ICompilablePropertyGetterFactory : IPropertyGetterFactory
    {
        /// <summary>
        /// This will return null if unable to return an ICompilablePropertyGetter for the named property that will return a value as the requested type
        /// </summary>
        new ICompilablePropertyGetter Get(Type srcType, string propertyName, Type destPropertyType);
    }
}
