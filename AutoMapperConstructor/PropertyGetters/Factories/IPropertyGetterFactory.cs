using System;

namespace AutoMapperConstructor.PropertyGetters.Factories
{
    public interface IPropertyGetterFactory
    {
        IPropertyGetter Get(Type srcType, string propertyName, Type destPropertyType);
    }
}
