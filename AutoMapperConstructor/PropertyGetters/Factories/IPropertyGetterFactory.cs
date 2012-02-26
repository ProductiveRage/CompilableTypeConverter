using System;

namespace CompilableTypeConverter.PropertyGetters.Factories
{
    public interface IPropertyGetterFactory
    {
        /// <summary>
        /// This will return null if unable to return an IPropertyGetter for the named property that will return a value as the requested type
        /// </summary>
        IPropertyGetter Get(Type srcType, string propertyName, Type destPropertyType);
    }
}
