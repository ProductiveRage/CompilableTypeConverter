using System;
using System.Reflection;
﻿using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Factories;
using CompilableTypeConverter.TypeConverters.Factories;

namespace DemoApp.CompiledExamples
{
    public class Examples
    {
        public static void TestConstructor()
        {
            // Prepare a converter factory using the base types (AssignableType and EnumConversion property getter factories)
            var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
            var converterFactory = ExtendableCompilableTypeConverterFactoryHelpers.GenerateConstructorBasedFactory(
                nameMatcher,
                new ArgsLengthTypeConverterPrioritiserFactory(),
                new ICompilablePropertyGetterFactory[]
                {
                    new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
                    new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
                }
            );

            // Extend the converter to handle SourceType.Sub1 to ConstructorDestType.Sub1 and IEnumerable<SourceType.Sub1> to IEnumerable<ConstructorDestType.Sub1>
            // - This will raise an exception if unable to create the mapping
            converterFactory = converterFactory.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();

            // This will enable the creation of a converter for SourceType to ConstructorDestType
            // - This will throw an exception if unable to generate an appropriate converter
            var converter = converterFactory.Get<SourceType, ConstructorDestType>();

            var result = converter.Convert(getExampleSourceType());
        }

        public static void TestPropertySetting()
        {
            // Prepare a converter factory using the base types (AssignableType and EnumConversion property getter factories)
            var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
            var converterFactory = ExtendableCompilableTypeConverterFactoryHelpers.GeneratePropertySetterBasedFactory(
                nameMatcher,
                CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions.MatchAsManyAsPossible,
                new ICompilablePropertyGetterFactory[]
                {
                    new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
                    new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
                },
				new PropertyInfo[0] // propertiesToIgnore
            );

            // Extend the converter to handle SourceType.Sub1 to ConstructorDestType.Sub1 and IEnumerable<SourceType.Sub1> to IEnumerable<ConstructorDestType.Sub1>
            // - This will raise an exception if unable to create the mapping
            converterFactory = converterFactory.CreateMap<SourceType.Sub1, StandardDestType.Sub1>();

            // This will enable the creation of a converter for SourceType to ConstructorDestType
            // - This will throw an exception if unable to generate an appropriate converter
			var converter = converterFactory.Get<SourceType, StandardDestType>();

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
                    null,
                    new SourceType.Sub1() { Name = "Bo3" }
                },
                ValueEnum = SourceType.Sub2.EnumValue2
            };
        }
    }
}
