using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapperConstructor.ConstructorInvokers;
using AutoMapperConstructor.ConstructorInvokers.Factories;
using AutoMapperConstructor.PropertyGetters;

namespace AutoMapperConstructor.TypeConverters
{
    /// <summary>
    /// A class capable of converting an instance of one type into another by calling a constructor on the target type - the manner in which the
    /// data is retrieved (and converted, if required) from the source type is determined by the provided IConstructorInvokerFactory instances
    /// (the way in which the target constructor is executed is determined by the specified IConstructorInvokerFactory; eg. the constructor
    /// may have its Invoke method called or IL code may be generated to call it)
    /// </summary>
    public class SimpleTypeConverterByConstructor<TSource, TDest> : ITypeConverterByConstructor<TSource, TDest>
    {
        private ConstructorInfo _constructor;
        private IConstructorInvoker<TDest> _constructorInvoker;
        private List<IPropertyGetter> _propertyGetters;
        public SimpleTypeConverterByConstructor(
            ConstructorInfo constructor,
            IEnumerable<IPropertyGetter> propertyGetters,
            IConstructorInvokerFactory constructorInvokerFactory)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            if (propertyGetters == null)
                throw new ArgumentNullException("propertyGetters");
            if (constructorInvokerFactory == null)
                throw new ArgumentNullException("constructorInvokerFactory");

            var propertyGettersList = new List<IPropertyGetter>();
            foreach (var propertyGetter in propertyGetters)
            {
                if (propertyGetter == null)
                    throw new ArgumentException("Null reference encountered in propertyGetters list");
                if (!propertyGetter.SrcType.Equals(typeof(TSource)))
                    throw new ArgumentException("Encountered invalid SrcType in propertyGetters list, must match type param U");
                propertyGettersList.Add(propertyGetter);
            }
            if (propertyGettersList.Count != constructor.GetParameters().Length)
                throw new ArgumentException("Number of propertyGetters.Count must match constructor.GetParameters().Length");

            _constructor = constructor;
            _constructorInvoker = constructorInvokerFactory.Get<TDest>(constructor);
            _propertyGetters = propertyGettersList;
        }

        /// <summary>
        /// The constructor method on the target type that will be used by the Convert method, this will never be null
        /// </summary>
        public ConstructorInfo Constructor { get { return _constructor; } }

        /// <summary>
        /// This will never be null nor contain any null enties, its number of entries will match the number of arguments the constructor has
        /// </summary>
        public IEnumerable<PropertyInfo> SrcProperties
        {
            get
            {
                return _propertyGetters.Select<IPropertyGetter, PropertyInfo>(p => p.Property);
            }
        }

        /// <summary>
        /// The type of the object that will be translated from - calls to the Convert method must specify objects of this type
        /// </summary>
        public Type SrcType
        {
            get { return typeof(TSource); }
        }

        /// <summary>
        /// The type of object to be translated into
        /// </summary>
        public Type DestType
        {
            get { return typeof(TDest); }
        }

        /// <summary>
        /// Create a new target type instance from a source value - this will never return null, it will throw an exception for null input, for a src object
        /// whose type does not match SrcType or if the conversion fails
        /// </summary>
        public object Convert(object src)
        {
            if (src == null)
                throw new ArgumentNullException("src");
            if (!src.GetType().Equals(SrcType))
                throw new ArgumentException("The type of src value must match SrcType");
            return Convert((TSource)src);
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

        /// <summary>
        /// Return a TypeParam'd ITypeConverterByConstructor instance - this will throw an exception if TSource does not equal SrcType or TDest does not
        /// equal DestType
        /// </summary>
        public ITypeConverterByConstructor<X, Y> AsGeneric<X, Y>()
        {
            if (!typeof(X).Equals(SrcType))
                throw new ArgumentException("Typeparam X must match SrcType");
            if (!typeof(Y).Equals(DestType))
                throw new ArgumentException("Typeparam X must match DestType");
            return (ITypeConverterByConstructor<X, Y>)this;
        }
    }
}
