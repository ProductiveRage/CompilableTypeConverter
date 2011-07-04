using System;
﻿using System.Collections.Generic;
﻿using System.Linq.Expressions;
﻿using AutoMapperConstructor.ConstructorPrioritisers.Factories;
using AutoMapperConstructor.NameMatchers;
using AutoMapperConstructor.PropertyGetters.Factories;
using AutoMapperConstructor.TypeConverters;
using AutoMapperConstructor.TypeConverters.Factories;

namespace DemoApp.CompiledExamples
{
    public class Examples
    {
        public static void Test()
        {
            // Prepare a converter factory using the base types (AssignableType and EnumConversion property getter factories)
            var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
            var converterFactory = new ExtendableCompilableTypeConverterFactory(
                nameMatcher,
                new ArgsLengthTypeConverterPrioritiserFactory(),
                new List<ICompilablePropertyGetterFactory>
                {
                    new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
                    new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
                }
            );

            // Extend the converter to handle SourceType.Sub1 to ConstructorDestType.Sub1 and IEnumerable<SourceType.Sub1> to IEnumerable<ConstructorDestType.Sub1>
            converterFactory = converterFactory.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();

            // This will enable the creation of a converter for SourceType to ConstructorDestType
            var converter = converterFactory.Get<SourceType, ConstructorDestType>();
            var result = converter.Convert(getExampleSourceType());
        }

        private static SourceType getExampleSourceType()
        {
            return new SourceType()
            {
                Value = new SourceType.Sub1() { Name = "Bo1" },
                //ValueList = null,
                ValueList = new[]
                {
                    new SourceType.Sub1() { Name = "Bo2" },
                    null,
                    new SourceType.Sub1() { Name = "Bo3" }
                },
                /*
                 */
                ValueEnum = SourceType.Sub2.EnumValue2
            };
        }
    }
}
