using System;
using System.Collections.Generic;
using System.Reflection;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Factories;

namespace CompilableTypeConverter.TypeConverters.Factories
{
    /// <summary>
    /// These are methods to try to remove one level of abstraction from ExtendableCompilableTypeConverterFactory for some common cases
    /// </summary>
    public static class ExtendableCompilableTypeConverterFactoryHelpers
    {
        /// <summary>
        /// This will return an ExtendableCompilableTypeConverterFactory that is based around the destination types being instantiated by constructor
        /// </summary>
        public static ExtendableCompilableTypeConverterFactory GenerateConstructorBasedFactory(
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

            return new ExtendableCompilableTypeConverterFactory(
                nameMatcher,
                basePropertyGetterFactories,
                propertyGetterFactories => new CompilableTypeConverterByConstructorFactory(
                    // Define a ConverterFactoryGenerator to return a CompilableTypeConverterByConstructorFactory when new conversions are registered
                    converterPrioritiser,
                    new CombinedCompilablePropertyGetterFactory(propertyGetterFactories),
					ParameterLessConstructorBehaviourOptions.Ignore
                ),
                new CompilableTypeConverterPropertyGetterFactoryExtrapolator(nameMatcher)
            );
        }

        /// <summary>
        /// This will return an ExtendableCompilableTypeConverterFactory based around the destination types having a zero parameter constructor and
        /// its data being set through public properties
        /// </summary>
        public static ExtendableCompilableTypeConverterFactory GeneratePropertySetterBasedFactory(
            INameMatcher nameMatcher,
            CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions propertySettingTypeOptions,
            IEnumerable<ICompilablePropertyGetterFactory> basePropertyGetterFactories,
			IEnumerable<PropertyInfo> propertiesToIgnore,
			ByPropertySettingNullSourceBehaviourOptions nullSourceBehaviour)
        {
            if (nameMatcher == null)
                throw new ArgumentNullException("nameMatcher");
            if (!Enum.IsDefined(typeof(CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions), propertySettingTypeOptions))
                throw new ArgumentOutOfRangeException("propertySettingTypeOptions");
            if (basePropertyGetterFactories == null)
                throw new ArgumentNullException("basePropertyGetterFactories");
			if (propertiesToIgnore == null)
				throw new ArgumentNullException("propertiesToIgnore");
			if (!Enum.IsDefined(typeof(ByPropertySettingNullSourceBehaviourOptions), nullSourceBehaviour))
				throw new ArgumentOutOfRangeException("nullSourceBehaviour");

            return new ExtendableCompilableTypeConverterFactory(
                nameMatcher,
                basePropertyGetterFactories,
                propertyGetterFactories => new CompilableTypeConverterByPropertySettingFactory(
                    // Define a ConverterFactoryGenerator to return a CompilableTypeConverterByPropertySettingFactory when new conversions are registered
                    new CombinedCompilablePropertyGetterFactory(propertyGetterFactories),
                    propertySettingTypeOptions,
					propertiesToIgnore,
					nullSourceBehaviour
                ),
                new CompilableTypeConverterPropertyGetterFactoryExtrapolator(nameMatcher)
            );
        }

        /// <summary>
        /// This IPropertyGetterFactoryExtrapolator implementations will add the ListCompilablePropertyGetterFactory to the mix - so each time CreateMap or
        /// AddNewConverter is called on the ExtendableCompilableTypeConverterFactory its internal list of conversions will extend to include that conversion
        /// and also the conversion of sets of that conversion (eg. if a SrcType:DestType conversion is added then an IEnumerable-SrcType:List-DestType
        /// conversion will also be added to the list).
        /// </summary>
        private class CompilableTypeConverterPropertyGetterFactoryExtrapolator : ExtendableCompilableTypeConverterFactory.IPropertyGetterFactoryExtrapolator
        {
            private INameMatcher _nameMatcher;
            public CompilableTypeConverterPropertyGetterFactoryExtrapolator(INameMatcher nameMatcher)
            {
                if (nameMatcher == null)
                    throw new ArgumentNullException("nameMatcher");

                _nameMatcher = nameMatcher;
            }

            /// <summary>
            /// This must never return null nor may any null entries be in the returned set. If will never be called with a null nameMatcher or converter reference.
            /// </summary>
            public IEnumerable<ICompilablePropertyGetterFactory> Get<TSource, TDest>(ICompilableTypeConverter<TSource, TDest> converter)
            {
                if (converter == null)
                    throw new ArgumentNullException("converter");

                return new ICompilablePropertyGetterFactory[]
                {
                    new ListCompilablePropertyGetterFactory<TSource, TDest>(
                        _nameMatcher,
                        converter
                    )
                };
            }
        }
    }
}
