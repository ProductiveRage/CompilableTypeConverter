using System.Collections.Generic;
﻿using AutoMapperConstructor.ConstructorPrioritisers.Factories;
using AutoMapperConstructor.NameMatchers;
using AutoMapperConstructor.PropertyGetters.Factories;
using AutoMapperConstructor.TypeConverters.Factories;

namespace DemoApp.CompiledExamples
{
    public class Examples
    {
        public static void Test()
        {
            // Prepare a basic type converter factory using AssignableTypes as Enums
            var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
            var prioritiserFactory = new ArgsLengthTypeConverterPrioritiserFactory();
            var propertyGetterFactories = new List<ICompilablePropertyGetterFactory>
            {
                new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
                new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
            };
            var converterFactory = new CompilableTypeConverterByConstructorFactory(
                prioritiserFactory,
                new CombinedCompilablePropertyGetterFactory(propertyGetterFactories)
            );

            // Generate a converter for SourceType.Sub1 to ConstructorDestType.Sub1 and use it to create property getter factories which will be able to handle property
            // retrieval of both SourceType.Sub1 to ConstructorDestType.Sub1 and IEnumerable<SourceType.Sub1> to IEnumerable<ConstructorDestType.Sub1>
            var converterSub1 = converterFactory.Get<SourceType.Sub1, ConstructorDestType.Sub1>();
            propertyGetterFactories.Add(
                new CompilableTypeConverterPropertyGetterFactory<SourceType.Sub1, ConstructorDestType.Sub1>(
                    nameMatcher,
                    converterSub1
                )
            );
            propertyGetterFactories.Add(
                new ListCompilablePropertyGetterFactory<SourceType.Sub1, ConstructorDestType.Sub1>(
                    nameMatcher,
                    converterSub1
                )
            );

            // Get new converter factory reference that incorporates the new property getters
            converterFactory = new CompilableTypeConverterByConstructorFactory(
                prioritiserFactory,
                new CombinedCompilablePropertyGetterFactory(propertyGetterFactories)
            );

            // This will enable the creation of a converter for SourceType to ConstructorDestType
            var converter = converterFactory.Get<SourceType, ConstructorDestType>();
            var result = converter.Convert(getExampleSourceType());
        }

        private static SourceType getExampleSourceType()
        {
            return new SourceType()
            {
                Value = new SourceType.Sub1() { Name = "Bo1" },
                ValueList = new[]
                {
                    new SourceType.Sub1() { Name = "Bo2" },
                    new SourceType.Sub1() { Name = "Bo3" }
                },
                ValueEnum = SourceType.Sub2.EnumValue2
            };
        }
    }
}
