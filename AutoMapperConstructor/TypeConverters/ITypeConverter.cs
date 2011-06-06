using System;
using System.Reflection;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input or if the
        /// conversion fails
        /// </summary>
        TDest Convert(TSource src);
    }
}
