﻿using System;
using System.Collections.Generic;
using System.Linq;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Factories;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    /// <summary>
    /// This forms the basis to construct an extendable compilable type converter factory - it will start with a small set of property getter factories that are sufficient
    /// to generate some converters which can then be fed back in to generate further converters whose properties are of the previous generators' types. By default any
    /// mapping will be wrapped in a CompilableTypeConverterPropertyGetterFactory (such that properties can be retrieved and converted using that mapping) but additional
    /// property getter factories may be generated by the specified PropertyGetterFactoryExtrapolator (eg. there might be factories which retrieve properties that are
    /// sets of the from-type and return a list of to-type). The ICompilableTypeConverterFactory.Get method requires ConverterFactoryGenerator delegate be specified
    /// such that the required converter factory can be initialised with all the built-up set of converting property getters (the returned ICompilableTypeConverter
    /// may be based upon constructors or property-setting, it is up the instantiator of this class to determine that).
    /// </summary>
    public class ExtendableCompilableTypeConverterFactory : ICompilableTypeConverterFactory
    {
        private INameMatcher _nameMatcher;
        private List<ICompilablePropertyGetterFactory> _basePropertyGetterFactories;
        private ConverterFactoryGenerator _converterFactoryGenerator;
        private IPropertyGetterFactoryExtrapolator _propertyGetterFactoryExtrapolator;
        private Lazy<ICompilableTypeConverterFactory> _typeConverterFactory;
        public ExtendableCompilableTypeConverterFactory(
            INameMatcher nameMatcher,
            IEnumerable<ICompilablePropertyGetterFactory> basePropertyGetterFactories,
            ConverterFactoryGenerator converterFactoryGenerator,
            IPropertyGetterFactoryExtrapolator propertyGetterFactoryExtrapolator)
        {
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (basePropertyGetterFactories == null)
                throw new ArgumentNullException("basePropertyGetterFactories");
            if (converterFactoryGenerator == null)
                throw new ArgumentNullException("converterFactoryGenerator");
            if (propertyGetterFactoryExtrapolator == null)
                throw new ArgumentNullException("propertyGetterFactoryExtrapolator");

            var basePropertyGetterFactoryList = new List<ICompilablePropertyGetterFactory>();
            foreach (var basePropertyGetterFactory in basePropertyGetterFactories)
            {
                if (basePropertyGetterFactory == null)
                    throw new ArgumentException("Null entry encountered in basePropertyGetterFactories");
                basePropertyGetterFactoryList.Add(basePropertyGetterFactory);
            }

            _nameMatcher = nameMatcher;
            _basePropertyGetterFactories = basePropertyGetterFactoryList;
            _converterFactoryGenerator = converterFactoryGenerator;
            _propertyGetterFactoryExtrapolator = propertyGetterFactoryExtrapolator;
            _typeConverterFactory = new Lazy<ICompilableTypeConverterFactory>(
                () =>
                {
                    var compilableTypeConverterFactory = converterFactoryGenerator(_basePropertyGetterFactories);
                    if (compilableTypeConverterFactory == null)
                        throw new Exception("Specified converterFactoryGenerator returned null");
                    return compilableTypeConverterFactory;
                },
                true
            );
        }

        /// <summary>
        /// This must never return null nor may it return a set containing any null entries. It will never be called with a null propertyGetterFactories reference.
        /// </summary>
        public delegate ICompilableTypeConverterFactory ConverterFactoryGenerator(IEnumerable<ICompilablePropertyGetterFactory> propertyGetterFactories);

        public interface IPropertyGetterFactoryExtrapolator
        {
            /// <summary>
            /// This must never return null nor may any null entries be in the returned set. If will never be called with a null converter reference.
            /// </summary>
            IEnumerable<ICompilablePropertyGetterFactory> Get<TSource, TDest>(ICompilableTypeConverter<TSource, TDest> converter);
        }

        /// <summary>
        /// This will return null if a converter could not be generated
        /// </summary>
        public ICompilableTypeConverter<TSource, TDest> Get<TSource, TDest>()
        {
            return _typeConverterFactory.Value.Get<TSource, TDest>();
        }

        ITypeConverter<TSource, TDest> ITypeConverterFactory.Get<TSource, TDest>()
        {
            return Get<TSource, TDest>();
        }

		/// <summary>
		/// This will throw an exception if unable to generate the requested mapping - it will never return null. If successful, the returned converter
		/// factory will be able to convert instances of TSourceNew as well as IEnumerable / Lists of them.
		/// </summary>
		public ExtendableCompilableTypeConverterFactory CreateMap<TSource, TDest>()
		{
			// Try to generate a converter for the requested mapping
			var converterNew = _typeConverterFactory.Value.Get<TSource, TDest>();
			if (converterNew == null)
				throw new Exception("Unable to create mapping");
			return AddNewConverter<TSource, TDest>(converterNew);
		}

		/// <summary>
		/// This will return null if unable to generate the requested converter. If successful, the returned converter must be passed back into the
		/// AddNewConverter method in order to generate a new instance of this class that is away of the converter (unlike the CreateMap method
		/// which will return a new instance of this class if it is successful).
		/// </summary>
		public ICompilableTypeConverter<TSource, TDest> TryToGenerateConverter<TSource, TDest>()
		{
			return _typeConverterFactory.Value.Get<TSource, TDest>();
		}

		/// <summary>
        /// Generate a further extended converter factory that will be able to handle conversion of instances of TSourceNew as well as IEnumerable / Lists
        /// of them. This will never return null.
        /// </summary>
        public ExtendableCompilableTypeConverterFactory AddNewConverter<TSource, TDest>(ICompilableTypeConverter<TSource, TDest> converterNew)
        {
            if (converterNew == null)
                throw new ArgumentNullException("converterNew");

            // Get any additional, extrapolated property getter factories (ensure that neither null or a set containing nulls is generated)
            var extrapolatedPropertyGetterFactories = _propertyGetterFactoryExtrapolator.Get<TSource, TDest>(converterNew);
            if (extrapolatedPropertyGetterFactories == null)
                throw new Exception("propertyGetterFactoryExtrapolator (" + _propertyGetterFactoryExtrapolator.GetType().ToString() + ") returned null");
            var extrapolatedPropertyGetterFactoriesList = new List<ICompilablePropertyGetterFactory>(extrapolatedPropertyGetterFactories);
            if (extrapolatedPropertyGetterFactoriesList.Any(f => f == null))
                throw new Exception("propertyGetterFactoryExtrapolator (" + _propertyGetterFactoryExtrapolator.GetType().ToString() + ") returned a null reference in the set");

            // Create a property getter factory that retrieves and convert properties using this converter by default, combine with any additional factories
            // generated by the propertyGetterFactoryExtrapolator and then..
            var extendedPropertyGetterFactories = new List<ICompilablePropertyGetterFactory>(_basePropertyGetterFactories);
            extendedPropertyGetterFactories.Add(
                new CompilableTypeConverterPropertyGetterFactory<TSource, TDest>(
                    _nameMatcher,
                    converterNew
                )
            );
            extendedPropertyGetterFactories.AddRange(extrapolatedPropertyGetterFactoriesList);

            // .. return a new ExtendableCompilableTypeConverterFactory that can make use of these new property getter factories
            return new ExtendableCompilableTypeConverterFactory(
                _nameMatcher,
                extendedPropertyGetterFactories,
                _converterFactoryGenerator,
                _propertyGetterFactoryExtrapolator
            );
        }
    }
}
