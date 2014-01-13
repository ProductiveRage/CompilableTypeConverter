using System;
using System.Collections.Generic;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverter.ConstructorPrioritisers
{
    public interface ITypeConverterPrioritiser<TSource, TDest>
    {
        /// <summary>
        /// Return the best matching ITypeConverterByConstructor reference with the most parameters - this will return null if no ITypeConverterByConstructors are
        /// specified or if none of the options are acceptable (allowing this to also effectively act as a type converter filter). It will throw an exception for
		/// null input or if the options data contains any null references.
        /// </summary>
        ITypeConverterByConstructor<TSource, TDest> Get(IEnumerable<ITypeConverterByConstructor<TSource, TDest>> options);
    }
}
