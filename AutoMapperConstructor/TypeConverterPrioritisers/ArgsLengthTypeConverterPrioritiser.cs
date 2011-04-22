using System;
using System.Collections.Generic;
using AutoMapperConstructor.TypeConverters;

namespace AutoMapperConstructor.ConstructorPrioritisers
{
    public class ArgsLengthTypeConverterPrioritiser : ITypeConverterPrioritiser
    {
        /// <summary>
        /// Return the TypeConverter reference with the most parameters
        /// </summary>
        public ITypeConverterByConstructor Get(IEnumerable<ITypeConverterByConstructor> options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            var optionsList = new List<ITypeConverterByConstructor>();
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
                    delegate(ITypeConverterByConstructor x, ITypeConverterByConstructor y)
                    {
                        return x.Constructor.GetParameters().Length.CompareTo(y.Constructor.GetParameters().Length);
                    }
                );
            }
            return optionsList[optionsList.Count - 1];
        }
    }
}
