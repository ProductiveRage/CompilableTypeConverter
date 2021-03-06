﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProductiveRage.CompilableTypeConverter.TypeConverters;

namespace ProductiveRage.CompilableTypeConverter.ConstructorPrioritisers
{
	public class ArgsLengthTypeConverterPrioritiser<TSource, TDest> : ITypeConverterPrioritiser<TSource, TDest>
    {
        /// <summary>
		/// Return the best matching ITypeConverterByConstructor reference with the most parameters - this will return null if no ITypeConverterByConstructors are
		/// specified or if none of the options are acceptable (allowing this to also effectively act as a type converter filter). It will throw an exception for
		/// null input or if the options data contains any null references.
		/// </summary>
        public ITypeConverterByConstructor<TSource, TDest> Get(IEnumerable<ITypeConverterByConstructor<TSource, TDest>> options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            var optionsList = new List<ITypeConverterByConstructor<TSource, TDest>>();
            foreach (var option in options)
            {
                if (option == null)
                    throw new ArgumentException("Null reference encountered in options data");
                optionsList.Add(option);
            }
            if (optionsList.Count == 0)
                return null;

            // For by-converter translations, the number of fulfilled constructor arguments (that aren't relying upon default argument values) is equal
			// to the number of matches properties
			if (optionsList.Count > 1)
            {
                optionsList.Sort(
					(x, y) => x.PropertyMappings.Count().CompareTo(y.PropertyMappings.Count())
                );
            }
            return optionsList[optionsList.Count - 1];
        }
    }
}
