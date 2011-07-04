using System;
using System.Collections.Generic;
using AutoMapperConstructor.ConstructorPrioritisers.Factories;
using AutoMapperConstructor.NameMatchers;
using AutoMapperConstructor.PropertyGetters.Factories;

namespace AutoMapperConstructor.TypeConverters.Factories
{
    /// <summary>
    /// An extendable compilable type converter factory - start with a small set of property getter factories that are sufficient to generate some converter, then feed
    /// back generated converter back to this to enable support of nested types. The converters fed in will extend the internal property getter factory list by adding
    /// CompilableTypeConverterPropertyGetterFactory and ListCompilablePropertyGetterFactory, so individual conversions of list of conversions will be supported.
    /// </summary>
    public class ExtendableCompilableTypeConverterFactory : ICompilableTypeConverterFactory
    {
        private INameMatcher _nameMatcher;
        private ITypeConverterPrioritiserFactory _converterPrioritiser;
        private List<ICompilablePropertyGetterFactory> _basePropertyGetterFactories;
        private Lazy<ICompilableTypeConverterFactory> _typeConverterFactory;
        public ExtendableCompilableTypeConverterFactory(
            INameMatcher nameMatcher,
            ITypeConverterPrioritiserFactory converterPrioritiser,
            IEnumerable<ICompilablePropertyGetterFactory> basePropertyGetterFactories)
        {
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (converterPrioritiser == null)
                throw new ArgumentNullException("converterPrioritiser");
            if (basePropertyGetterFactories == null)
                throw new ArgumentNullException("basePropertyGetterFactories");

            var basePropertyGetterFactoryList = new List<ICompilablePropertyGetterFactory>();
            foreach (var basePropertyGetterFactory in basePropertyGetterFactories)
            {
                if (basePropertyGetterFactory == null)
                    throw new ArgumentException("Null entry encountered in basePropertyGetterFactories");
                basePropertyGetterFactoryList.Add(basePropertyGetterFactory);
            }

            _nameMatcher = nameMatcher;
            _converterPrioritiser = converterPrioritiser;
            _basePropertyGetterFactories = basePropertyGetterFactoryList;
            _typeConverterFactory = new Lazy<ICompilableTypeConverterFactory>(getConverterFactory, true);
        }

        private ICompilableTypeConverterFactory getConverterFactory()
        {
            return new CompilableTypeConverterByConstructorFactory(
                _converterPrioritiser,
                new CombinedCompilablePropertyGetterFactory(_basePropertyGetterFactories)
            );
        }

        /// <summary>
        /// This will return null if a converter could not be generated
        /// </summary>
        public ICompilableTypeConverterByConstructor<TSource, TDest> Get<TSource, TDest>()
        {
            return _typeConverterFactory.Value.Get<TSource, TDest>();
        }

        ITypeConverter<TSource, TDest> ITypeConverterFactory.Get<TSource, TDest>()
        {
            return Get<TSource, TDest>();
        }

        /// <summary>
        /// This will throw an exception if unable to generate the requested mapping - it will never return null. If the successful, the returned converter
        /// factory will be able to convert instances of TSourceNew as well as IEnumerable / Lists of them.
        /// </summary>
        public ExtendableCompilableTypeConverterFactory CreateMap<TSourceNew, TDestNew>()
        {
            // Try to generate a converter for the requested mapping
            var converterNew = _typeConverterFactory.Value.Get<TSourceNew, TDestNew>();
            if (converterNew == null)
                throw new Exception("Unable to create mapping");
            return AddNewConverter<TSourceNew, TDestNew>(converterNew);
        }

        /// <summary>
        /// Generate a further extended converter factory that will be able to handle conversion of instances of TSourceNew as well as IEnumerable / Lists
        /// of them. This will never return null.
        /// </summary>
        public ExtendableCompilableTypeConverterFactory AddNewConverter<TSourceNew, TDestNew>(ICompilableTypeConverter<TSourceNew, TDestNew> converterNew)
        {
            if (converterNew == null)
                throw new ArgumentNullException("converterNew");

            // Create a property getter factory that retrieves and convert properties using this converter and one that does the same for IEnumerable
            // properties, where the IEnumerables' elements are the types handled by the converter
            var extendedPropertyGetterFactories = new List<ICompilablePropertyGetterFactory>(_basePropertyGetterFactories);
            extendedPropertyGetterFactories.Add(
                new CompilableTypeConverterPropertyGetterFactory<TSourceNew, TDestNew>(
                    _nameMatcher,
                    converterNew
                )
            );
            extendedPropertyGetterFactories.Add(
                new ListCompilablePropertyGetterFactory<TSourceNew, TDestNew>(
                    _nameMatcher,
                    converterNew
                )
            );

            // Return a new ExtendableCompilableTypeConverterFactory that can make use of these new property getter factories
            return new ExtendableCompilableTypeConverterFactory(
                _nameMatcher,
                _converterPrioritiser,
                extendedPropertyGetterFactories
            );
        }
    }
}
