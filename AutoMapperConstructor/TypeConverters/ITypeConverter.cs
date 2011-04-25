using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ITypeConverterByConstructor<TSource, TDest>
    {
        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input or if the
        /// conversion fails
        /// </summary>
        TDest Convert(TSource src);

        /// <summary>
        /// The constructor method on the target type that will be used by the Convert method, this will never be null
        /// </summary>
        ConstructorInfo Constructor { get; }

        /// <summary>
        /// This will never be null nor contain any null enties, its number of entries will match the number of arguments the constructor has
        /// </summary>
        IEnumerable<PropertyInfo> SrcProperties { get; }
    }
}
