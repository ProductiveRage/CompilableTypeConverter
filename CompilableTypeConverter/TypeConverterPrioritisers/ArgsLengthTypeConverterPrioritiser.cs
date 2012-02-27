using System;
using System.Collections.Generic;
using CompilableTypeConverter.TypeConverters;

namespace CompilableTypeConverter.ConstructorPrioritisers
{
    public class ArgsLengthTypeConverterPrioritiser<TSource, TDest> : ITypeConverterPrioritiser<TSource, TDest>
    {
        /// <summary>
        /// Return the TypeConverter reference with the most parameters - this will return null if no ITypeConverterByConstructors are specified, it will throw
        /// an exception for null input or if the options data contains any null references
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

            if (optionsList.Count > 1)
            {
                optionsList.Sort(
                    delegate(ITypeConverterByConstructor<TSource, TDest> x, ITypeConverterByConstructor<TSource, TDest> y)
                    {
                        return x.Constructor.GetParameters().Length.CompareTo(y.Constructor.GetParameters().Length);
                    }
                );
            }
            return optionsList[optionsList.Count - 1];
        }
    }
}
