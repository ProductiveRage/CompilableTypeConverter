using System;
using System.Collections.Generic;
using System.Reflection;
using AutoMapperConstructor.ConstructorInvokers;
using AutoMapperConstructor.PropertyGetters;

namespace AutoMapperConstructor.TypeConverters
{
    /// <summary>
    /// A class capable of converting an instance of one type into another by calling a constructor on the target type - the manner in which the
    /// data is retrieved (and converted, if required) from the source type is determined by the provided IPropertyGetter instances, the way in
    /// which the target constructor is executed is determined by the specified IConstructorInvoker; eg. the constructor may have its Invoke
    /// method called or IL code may be generated to call it)
    /// </summary>
    public class SimpleTypeConverterByConstructor<TSource, TDest> : ITypeConverterByConstructor<TSource, TDest>
    {
        private IConstructorInvoker<TDest> _constructorInvoker;
        private List<IPropertyGetter> _propertyGetters;
        public SimpleTypeConverterByConstructor(IEnumerable<IPropertyGetter> propertyGetters, IConstructorInvoker<TDest> constructorInvoker)
        {
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
            if (constructorInvoker == null)
                throw new ArgumentNullException("constructorInvoker");

            // Ensure there are no null references in the property getter content
            var propertyGettersList = new List<IPropertyGetter>();
            foreach (var propertyGetter in propertyGetters)
            {
                if (propertyGetter == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
                if (!propertyGetter.SrcType.Equals(typeof(TSource)))
                    throw new ArgumentException("Encountered invalid SrcType in propertyGetters list, must match type param U");
                propertyGettersList.Add(propertyGetter);
            }

            // Ensure that the property getters correspond to the constructor that's being targetted
            var constructorParameters = constructorInvoker.Constructor.GetParameters();
            if (propertyGettersList.Count != constructorParameters.Length)
                throw new ArgumentException("Number of propertyGetters.Count must match constructor.GetParameters().Length");
            for (var index = 0; index < propertyGettersList.Count; index++)
            {
                if (!constructorParameters[index].ParameterType.IsAssignableFrom(propertyGettersList[index].TargetType))
                    throw new ArgumentException("propertyGetter[" + index + "].TargetType is not assignable to corresponding constructor parameter type");
            }

            _constructorInvoker = constructorInvoker;
            _propertyGetters = propertyGettersList;
        }

        /// <summary>
        /// The destination Constructor must be exposed by ITypeConverterByConstructor so that ITypeConverterPrioritiser implementations have something to work
        /// with - this value will never be null
        /// </summary>
        public ConstructorInfo Constructor
        {
            get { return _constructorInvoker.Constructor; }
        }

        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input or if conversion fails
        /// </summary>
        public TDest Convert(TSource src)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            var args = new object[_propertyGetters.Count];
            for (var index = 0; index < _propertyGetters.Count; index++)
                args[index] = _propertyGetters[index].GetValue(src);
            return (TDest)_constructorInvoker.Invoke(args);
        }
    }
}
