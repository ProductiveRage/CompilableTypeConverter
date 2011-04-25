using System;
using System.Collections.Generic;
using AutoMapperConstructor.TypeConverters;

namespace AutoMapperConstructor.ConstructorPrioritisers
{
    public interface ITypeConverterPrioritiser<TSource, TDest>
    {
        /// <summary>
        /// Return the best matching ITypeConverterByConstructor reference with the most parameters - this will return null if no ITypeConverterByConstructors are
        /// specified, it will throw an exception for null input or if the options data contains any null references
        /// </summary>
        ITypeConverterByConstructor<TSource, TDest> Get(IEnumerable<ITypeConverterByConstructor<TSource, TDest>> options);
    }
}
