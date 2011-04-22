using System;

namespace AutoMapperConstructor.TypeConverters.Factories
{
    public interface ITypeConverterByConstructorFactory
    {
        /// <summary>
        /// This will return null if no suitable constructors were retrieved
        /// </summary>
        ITypeConverterByConstructor Get(Type srcType, Type destType);

        /// <summary>
        /// This will return null if no suitable constructors were retrieved
        /// </summary>
        ITypeConverterByConstructor<TSource, TDest> Get<TSource, TDest>();
    }
}
