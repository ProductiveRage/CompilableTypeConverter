using System;
using System.Collections.Generic;
using AutoMapperConstructor.TypeConverters;

namespace AutoMapperConstructor.ConstructorPrioritisers
{
    public interface ITypeConverterPrioritiser
    {
        ITypeConverterByConstructor Get(IEnumerable<ITypeConverterByConstructor> options);
    }
}
