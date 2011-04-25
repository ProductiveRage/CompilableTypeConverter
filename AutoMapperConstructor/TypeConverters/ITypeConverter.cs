using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoMapperConstructor.TypeConverters
{
    public interface ITypeConverterByConstructor<TSource, TDest> : ITypeConverterByConstructor
    {
        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input or if the
        /// conversion fails
        /// </summary>
        TDest Convert(TSource src);
    }

    public interface ITypeConverterByConstructor
    {
        /// <summary>
        /// The constructor method on the target type that will be used by the Convert method, this will never be null
        /// </summary>
        ConstructorInfo Constructor { get; }

        /// <summary>
        /// This will never be null nor contain any null enties, its number of entries will match the number of arguments the constructor has
        /// </summary>
        IEnumerable<PropertyInfo> SrcProperties { get; }

        /// <summary>
        /// The type of the object that will be translated from - calls to the Convert method must specify objects of this type
        /// </summary>
        Type SrcType { get; }

        /// <summary>
        /// The type of object to be translated into
        /// </summary>
        Type DestType { get; }

        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input, for a src object
        /// whose type does not match SrcType or if the conversion fails
        /// </summary>
        object Convert(object src);

        /// <summary>
        /// Return a TypeParam'd ITypeConverterByConstructor instance - this will throw an exception if TSource does not equal SrcType or TDest does not
        /// equal DestType
        /// </summary>
        ITypeConverterByConstructor<TSource, TDest> AsGeneric<TSource, TDest>();
    }
}
