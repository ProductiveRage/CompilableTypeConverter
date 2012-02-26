using System;
using AutoMapper;
using AutoMapperIntegration.PropertyGetters.Factories;
using CompilableTypeConverter.NameMatchers;
using NUnit.Framework;

namespace UnitTesting.PropertyGetters.Factories
{
    [TestFixture]
    public class AutoMapperEnabledPropertyGetterFactoryTests
    {
        // ======================================================================================================================
        // TESTS: Class Initialisation
        // ======================================================================================================================
        [Test]
        public void InitialisingWithNullNameMatcherShouldFail()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    var propertyGetterFactory = new AutoMapperEnabledPropertyGetterFactory(
                        null,
                        getBasicAutoMapperConfiguration()
                    );
                },
                "Constructor should throw an exception for null nameMatcher"
            );
        }

        [Test]
        public void InitialisingWithNullMappingEngineShouldFail()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    var propertyGetterFactory = new AutoMapperEnabledPropertyGetterFactory(
                        new NameMatcher((from, to) => false),
                        null
                    );
                },
                "Constructor should throw an exception for null mappingConfig"
            );
        }

        // ======================================================================================================================
        // TESTS: Retrieval without conversion
        // ======================================================================================================================
        [Test]
        public void RetrieveIntValuePropertyGetter_NoConversion()
        {
            var propertyGetterFactory = new AutoMapperEnabledPropertyGetterFactory(
                new NameMatcher((from, to) =>
                {
                    return from == "intValue" && to == "IntValue";
                }),
                getBasicAutoMapperConfiguration()
            );
            var propertyGetter = propertyGetterFactory.Get(
                typeof(SourceType),
                "intValue",
                typeof(int)
            );

            // Does the returned property getter appear valid?
            Assert.NotNull(propertyGetter);
            Assert.AreEqual(typeof(SourceType), propertyGetter.SrcType);
            Assert.AreEqual(typeof(SourceType).GetProperty("IntValue"), propertyGetter.Property);
            Assert.AreEqual(typeof(int), propertyGetter.TargetType);

            // Does it actually retrieve data correctly?
            var src = new SourceType() { IntValue = 3 };
            Assert.AreEqual(3, propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveIntValuePropertyGetter_AsSourceType()
        {
            var mappingConfig = getBasicAutoMapperConfiguration();
            mappingConfig.CreateMap<int, SourceType>().ConstructUsing(x => new SourceType() { IntValue = x });
            var propertyGetterFactory = new AutoMapperEnabledPropertyGetterFactory(
                new NameMatcher((from, to) =>
                {
                    return from == "intValue" && to == "IntValue";
                }),
                mappingConfig
            );
            var propertyGetter = propertyGetterFactory.Get(
                typeof(SourceType),
                "intValue",
                typeof(SourceType)
            );

            // Does the returned property getter appear valid?
            Assert.NotNull(propertyGetter);
            Assert.AreEqual(typeof(SourceType), propertyGetter.SrcType);
            Assert.AreEqual(typeof(SourceType).GetProperty("IntValue"), propertyGetter.Property);
            Assert.AreEqual(typeof(SourceType), propertyGetter.TargetType);

            // Does it actually retrieve data correctly?
            var src = new SourceType() { IntValue = 3 };
            var expected = new SourceType() { IntValue = 3 };
            Assert.True(Common.DoSerialisableObjectsHaveMatchingContent(expected, propertyGetter.GetValue(src)));
        }

        /// <summary>
        /// If the AutoMapperEnabledPropertyGetterFactory can't provide an appropriate getter, it should return null
        /// </summary>
        [Test]
        public void RetrieveIntValuePropertyGetter_AsSourceType_WithoutMapping()
        {
            var propertyGetterFactory = new AutoMapperEnabledPropertyGetterFactory(
                new NameMatcher((from, to) =>
                {
                    return from == "intValue" && to == "IntValue";
                }),
                getBasicAutoMapperConfiguration()
            );
            var propertyGetter = propertyGetterFactory.Get(
                typeof(SourceType),
                "intValue",
                typeof(SourceType)
            );

            // Does the returned property getter appear valid?
            Assert.Null(propertyGetter);
        }

        // ======================================================================================================================
        // COMMON
        // ======================================================================================================================
        [Serializable]
        private class SourceType
        {
            public int IntValue { get; set; }
        }

        private class NameMatcher : INameMatcher
        {
            public delegate bool Comparison(string from, string to);
            private Comparison _comparison;
            public NameMatcher(Comparison comparison)
            {
                if (comparison == null)
                    throw new ArgumentNullException("comparison");
                _comparison = comparison;
            }
            public bool IsMatch(string from, string to)
            {
                return _comparison(from, to);
            }
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
