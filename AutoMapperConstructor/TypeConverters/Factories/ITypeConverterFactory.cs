using System;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    public interface ITypeConverterFactory
    {
        /// <summary>
        /// This will return null if a converter could not be generated
        /// </summary>
        ITypeConverter<TSource, TDest> Get<TSource, TDest>();
    }
}
