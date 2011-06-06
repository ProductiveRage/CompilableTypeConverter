using System;
using System.Reflection;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ITypeConverterByConstructor<TSource, TDest> : ITypeConverter<TSource, TDest>
    {
        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null
        /// </summary>
        ConstructorInfo Constructor { get; }
    }
}
