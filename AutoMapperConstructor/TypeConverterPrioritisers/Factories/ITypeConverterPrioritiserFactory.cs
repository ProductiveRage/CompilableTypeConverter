using System;
using System.Collections.Generic;
using AutoMapperConstructor.TypeConverters;

namespace AutoMapperConstructor.ConstructorPrioritisers.Factories
{
    /// <summary>
    /// We require the ITypeConverterPrioritiserFactory as it may be used by an ITypeConverterByConstructorFactory implementation, and neither of these
    /// have typeparams - unlike the ITypeConverterByConstructor and ITypeConverterPrioritiser which do have typeparams and so need factories to create
    /// them for the required types
    /// </summary>
    public interface ITypeConverterPrioritiserFactory
    {
        /// <summary>
        /// This should never return null
        /// </summary>
        ITypeConverterPrioritiser<TSource, TDest> Get<TSource, TDest>();
    }
}
