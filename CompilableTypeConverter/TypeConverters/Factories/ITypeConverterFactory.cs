using System;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    public interface ITypeConverterFactory
    {
        /// <summary>
        /// This will throw an exception if a converter could not be generated, it will never return null
        /// </summary>
        ITypeConverter<TSource, TDest> Get<TSource, TDest>();
    }
}
