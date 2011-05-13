using System;
using System.Reflection;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ITypeConverterByConstructor<TSource, TDest>
    {
        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null
        /// </summary>
        ConstructorInfo Constructor { get; }

        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input or if the
        /// conversion fails
        /// </summary>
        TDest Convert(TSource src);
    }
}
