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
            var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
            var converterFactory = new CompilableTypeConverterByConstructorFactory(
                new ArgsLengthTypeConverterPrioritiserFactory(),
                new CombinedCompilablePropertyGetterFactory(
                    new ICompilablePropertyGetterFactory[]
                    {
                        new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
                        new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
                    }
                )
            );

            var converter = converterFactory.Get<SourceType, ConstructorDestType>();
            var result = converter.Convert(getExampleSourceType());
        }

        private static SourceType getExampleSourceType()
        {
            return new SourceType()
            {
                Value = "Bo1",
                ValueList = new[]
                {
                    "Bo2",
                    "Bo3"
                },
                ValueEnum = SourceType.EnumSub.EnumValue2
            };
        }
    }
}
