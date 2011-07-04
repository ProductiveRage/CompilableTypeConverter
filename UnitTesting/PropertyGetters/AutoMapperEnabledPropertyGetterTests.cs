using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using AutoMapperConstructor.PropertyGetters;
using NUnit.Framework;

namespace UnitTesting.PropertyGetters.Compilable
{
    [TestFixture]
    public class AutoMapperEnabledPropertyGetterTests
    {
        // ======================================================================================================================
        // TESTS: Class Initialisation
        // ======================================================================================================================
        [Test]
        public void InitialisingWithNullPropertyInfoShouldFail()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, string>(
                        null,
                        new MappingEngine(getBasicAutoMapperConfiguration())
                    );
                },
                "Constructor should throw an exception for null propertyInfo"
            );
        }

        [Test]
        public void InitialisingWithNullMappingEngineShouldFail()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, string>(
                        typeof(SourceType).GetProperty("IntValue"),
                        null
                    );
                },
                "Constructor should throw an exception for null mappingEngine"
            );
        }

        // ======================================================================================================================
        // TESTS: Retrieval without conversion
        // ======================================================================================================================
        [Test]
        public void RetrieveIntValue_NoConversion()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, int>(
                typeof(SourceType).GetProperty("IntValue"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValue = 3
            };
            Assert.AreEqual(3, propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveIntValueList_Null_NoConversion()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, int[]>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValueList = null
            };
            Assert.AreEqual(null, propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveIntValueList_Empty_NoConversion()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, int[]>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValueList = new int[0]
            };
            Assert.AreEqual(new int[0], propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveIntValueList_1_2_3_NoConversion()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, int[]>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValueList = new[] { 1, 2, 3 }
            };
            Assert.AreEqual(new[] { 1, 2, 3 }, propertyGetter.GetValue(src));
        }

        // ======================================================================================================================
        // TESTS: Retrieval with conversion
        // ======================================================================================================================
        [Test]
        public void RetrieveIntValue_AsString()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, string>(
                typeof(SourceType).GetProperty("IntValue"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValue = 3
            };
            Assert.AreEqual("3", propertyGetter.GetValue(src));
        }

        /// <summary>
        /// Test the use of a custom AutoMapper translation
        /// </summary>
        [Test]
        public void RetrieveIntValue_AsSourceType()
        {
            var mappingConfig = getBasicAutoMapperConfiguration();
            mappingConfig.CreateMap<int, SourceType>().ConstructUsing(x => new SourceType() { IntValue = x });
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, SourceType>(
                typeof(SourceType).GetProperty("IntValue"),
                new MappingEngine(mappingConfig)
            );
            var src = new SourceType()
            {
                IntValue = 3
            };
            var result = propertyGetter.GetValue(src);
            Assert.IsNotNull(result, "Converted property value should not be null");
            Assert.AreEqual(3, result.IntValue);
            Assert.True(
                Common.DoSerialisableObjectsHaveMatchingContent(
                    new SourceType() { IntValue = 3 },
                    result
                ),
                "Converted property value has unexpected data set"
            );
        }

        [Test]
        public void RetrieveIntValueList_Null_AsStringArray()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, IEnumerable<string>>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValueList = null
            };
            Assert.AreEqual(null, propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveIntValueList_Empty_AsStringArray()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, IEnumerable<string>>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValueList = new int[0]
            };
            Assert.AreEqual(
                new string[0],
                propertyGetter.GetValue(src)
            );
        }

        [Test]
        public void RetrieveIntValueList_1_2_3_AsStringArray()
        {
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, IEnumerable<string>>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(getBasicAutoMapperConfiguration())
            );
            var src = new SourceType()
            {
                IntValueList = new[] { 1, 2, 3 }
            };
            Assert.AreEqual(
                new[] { "1", "2", "3" },
                propertyGetter.GetValue(src)
            );
        }

        /// <summary>
        /// AutoMapper can conveniently support converting sets of a type once it has a mapping for the single conversion (so setting up a mapping from int to
        /// SourceType should mean that we're able to retrieve a property of int IEnumerable as SourceType IEnumerable)
        /// </summary>
        [Test]
        public void RetrieveIntValueList_1_2_3_AsSourceTypeArray()
        {
            var mappingConfig = getBasicAutoMapperConfiguration();
            mappingConfig.CreateMap<int, SourceType>().ConstructUsing(x => new SourceType() { IntValue = x });
            var propertyGetter = new AutoMapperEnabledPropertyGetter<SourceType, IEnumerable<SourceType>>(
                typeof(SourceType).GetProperty("IntValueList"),
                new MappingEngine(mappingConfig)
            );
            var src = new SourceType()
            {
                IntValueList = new[] { 1, 2, 3 }
            };
            var result = propertyGetter.GetValue(src);
            Assert.IsNotNull(result, "Converted property value should not be null");
            Assert.AreEqual(3, result.Count(), "Resulting IEnumerable<SourceType> should have three entries");
            var index = 0;
            foreach (var resultEntry in result)
            {
                var expectedIntValue = index + 1;
                Assert.AreEqual(expectedIntValue, resultEntry.IntValue, "Index " + index.ToString() + ": Unexpected IntValue");
                Assert.True(
                    Common.DoSerialisableObjectsHaveMatchingContent(
                        new SourceType() { IntValue = expectedIntValue },
                        resultEntry
                    ),
                    "Index " + index.ToString() + ": Converted property value has unexpected data set"
                );
                index++;
            }
        }

        // ======================================================================================================================
        // COMMON
        // ======================================================================================================================
        [Serializable]
        private class SourceType
        {
            public int IntValue { get; set; }
            public IEnumerable<int> IntValueList { get; set; }
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
