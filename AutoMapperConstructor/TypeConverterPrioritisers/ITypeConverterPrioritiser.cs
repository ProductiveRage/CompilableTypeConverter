using System;
using System.Collections.Generic;
using AutoMapperConstructor.TypeConverters;

namespace AutoMapperConstructor.ConstructorPrioritisers
{
    public interface ITypeConverterPrioritiser
    {
        /// <summary>
        /// Return the best matching ITypeConverterByConstructor reference with the most parameters - this will return null if no ITypeConverterByConstructors are
        /// specified, it will throw an exception for null input or if the options data contains any null references
        /// </summary>
        ITypeConverterByConstructor Get(IEnumerable<ITypeConverterByConstructor> options);
    }
}
