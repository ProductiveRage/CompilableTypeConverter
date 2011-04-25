using System;
using System.Collections.Generic;
using AutoMapper;
using AutoMapperConstructor.ConstructorInvokers.Factories;
using AutoMapperConstructor.ConstructorPrioritisers.Factories;
using AutoMapperConstructor.NameMatchers;
using AutoMapperConstructor.PropertyGetters.Factories;
using AutoMapperConstructor.TypeConverters.Factories;

namespace AutoMapperConstructor
{
    class Program
    {
        static void Main(string[] args)
        {
            var destStandard = getStandardAutoMapperTranslation(getExampleSourceType());
            var destConstructor = getStandardAutoMapperConstructor(getExampleSourceType());
        }

        private static StandardDestType getStandardAutoMapperTranslation(SourceType source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            // Get a no-frills, run-of-the-mill AutoMapper Configuration reference..
            var mapperConfig = getBasicAutoMapperConfiguration();

            // .. teach it the SourceType.Sub1 to DestType.Sub1 mapping..
            mapperConfig.CreateMap<SourceType.Sub1, StandardDestType.Sub1>();

            // .. and then the SourceType to StandardDestType mapping we really want
            mapperConfig.CreateMap<SourceType, StandardDestType>();

            // Let AutoMapper do its thing!
            return (new MappingEngine(mapperConfig)).Map<SourceType, StandardDestType>(getExampleSourceType());
        }

        private static ConstructorDestType getStandardAutoMapperConstructor(SourceType source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            // Get a no-frills, run-of-the-mill AutoMapper Configuration reference..
            var mapperConfig = getBasicAutoMapperConfiguration();

            // .. teach it the SourceType.Sub1 to DestType.Sub1 mapping (unfortunately AutoMapper can't magically handle nested types,
            // we have to do the same in the getStandardAutoMapperTranslation method)
            mapperConfig.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();

            // If the translatorFactory is unable to find any constructors it can use for the conversion, the translatorFactory.Get
            // method will return null
            var translatorFactory = new SimpleTypeConverterByConstructorFactory(
                new ArgsLengthTypeConverterPrioritiserFactory(),
                new SimpleConstructorInvokerFactory(),
                new AutoMapperEnabledPropertyGetterFactory(
                    new CaseInsensitiveSkipUnderscoreNameMatcher(),
                    mapperConfig
                )
            );
            var translator = translatorFactory.Get<SourceType, ConstructorDestType>();
            if (translator == null)
                throw new Exception("Unable to obtain a mapping");

            // Make our translation available to the AutoMapper configuration
            mapperConfig.CreateMap<SourceType, ConstructorDestType>().ConstructUsing(translator.Convert);

            // NOW let AutoMapper do its thing!
            return new MappingEngine(mapperConfig).Map<SourceType, ConstructorDestType>(source);
        }

        /// <summary>
        /// Get an example SourceType instance to attempt to convert (this will test nested types and transformed enums)
        /// </summary>
        private static SourceType getExampleSourceType()
        {
            return new SourceType()
            {
                Value = new SourceType.Sub1()
                {
                    Name = "Bo1"
                },
                EnumValue = SourceType.Sub2.EnumValue3,
                ValueList = new[]
                {
                    new SourceType.Sub1()
                    {
                        Name = "Bo2"
                    },
                    new SourceType.Sub1()
                    {
                        Name = "Bo3"
                    }
                }
            };
        }

        /// <summary>
        /// Get a bog-standard isolated AutoMapper configuration to play with
        /// </summary>
        private static Configuration getBasicAutoMapperConfiguration()
        {
            var mapperConfig = new Configuration(
                new TypeMapFactory(),
                AutoMapper.Mappers.MapperRegistry.AllMappers()
            );
            mapperConfig.SourceMemberNamingConvention = new LowerUnderscoreNamingConvention();
            return mapperConfig;
        }
    }
}
