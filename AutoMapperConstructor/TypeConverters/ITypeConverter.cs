using System;
using System.Reflection;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// Create a new target type instance from a source value - this will throw an exception if conversion fails
        /// </summary>
        TDest Convert(TSource src);
    }
}
