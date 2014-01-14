using System;
using System.Linq.Expressions;
using CompilableTypeConverter.PropertyGetters.Compilable;
using CompilableTypeConverter.TypeConverters;
using NUnit.Framework;

namespace UnitTesting.PropertyGetters
{
    [TestFixture]
    public class CompilableTypeConverterPropertyGetterTests
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
                    var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, int, int>(
                        null,
                        new NonConvertingCompilableIntTypeConverter()
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
                    var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, int, int>(
                        typeof(SourceType).GetProperty("IntValue"),
                        null
                    );
                },
                "Constructor should throw an exception for null compilableTypeConverter"
            );
        }

        // ======================================================================================================================
        // TESTS: Retrieval without conversion [int]
        // ======================================================================================================================
        [Test]
        public void RetrieveIntValue_3_NoConversion()
        {
            var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, int, int>(
                typeof(SourceType).GetProperty("IntValue"),
                new NonConvertingCompilableIntTypeConverter()
            );
            var src = new SourceType()
            {
                IntValue = 3
            };
            Assert.AreEqual(3, propertyGetter.GetValue(src));
        }

        // ======================================================================================================================
        // TESTS: Retrieval without conversion [int]
        // ======================================================================================================================
        [Test]
        public void RetrieveStringValue_Null_NoConversion()
        {
            var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, string, string>(
                typeof(SourceType).GetProperty("StringValue"),
                new NonConvertingCompilableStringTypeConverter()
            );
            var src = new SourceType()
            {
                StringValue = null
            };
            Assert.AreEqual(null, propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveStringValue_Empty_NoConversion()
        {
            var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, string, string>(
                typeof(SourceType).GetProperty("StringValue"),
                new NonConvertingCompilableStringTypeConverter()
            );
            var src = new SourceType()
            {
                StringValue = ""
            };
            Assert.AreEqual("", propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveStringValue_WhiteSpace_NoConversion()
        {
            var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, string, string>(
                typeof(SourceType).GetProperty("StringValue"),
                new NonConvertingCompilableStringTypeConverter()
            );
            var src = new SourceType()
            {
                StringValue = "  \r\n   "
            };
            Assert.AreEqual("  \r\n   ", propertyGetter.GetValue(src));
        }

        [Test]
        public void RetrieveStringValue_3_NoConversion()
        {
            var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, string, string>(
                typeof(SourceType).GetProperty("StringValue"),
                new NonConvertingCompilableStringTypeConverter()
            );
            var src = new SourceType()
            {
                StringValue = "3"
            };
            Assert.AreEqual("3", propertyGetter.GetValue(src));
        }

        // ======================================================================================================================
        // TESTS: Retrieval [int to string conversion]
        // ======================================================================================================================
        [Test]
        public void RetrieveIntValue_3_AsString()
        {
            var propertyGetter = new CompilableTypeConverterPropertyGetter<SourceType, int, string>(
                typeof(SourceType).GetProperty("IntValue"),
                new CompilableIntToStringTypeConverter()
            );
            var src = new SourceType()
            {
                IntValue = 3
            };
            Assert.AreEqual("3", propertyGetter.GetValue(src));
        }

        // ======================================================================================================================
        // COMMON
        // ======================================================================================================================
        [Serializable]
        private class SourceType
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; }
        }

        private class NonConvertingCompilableIntTypeConverter : ICompilableTypeConverter<int, int>
        {
            public int Convert(int src)
            {
                return src;
            }
            public Expression GetTypeConverterExpression(Expression param)
            {
                if (param == null)
                    throw new ArgumentNullException("param");
                return param;
            }
			public Expression<Func<int, int>> GetTypeConverterFuncExpression()
			{
				var srcParameter = Expression.Parameter(typeof(int), "src");
				return Expression.Lambda<Func<int, int>>(
					GetTypeConverterExpression(srcParameter),
					srcParameter
				);
			}
		}

        private class NonConvertingCompilableStringTypeConverter : ICompilableTypeConverter<string, string>
        {
            public string Convert(string src)
            {
                return src;
            }
            public Expression GetTypeConverterExpression(Expression param)
            {
                if (param == null)
                    throw new ArgumentNullException("param");
                return param;
            }
			public Expression<Func<string, string>> GetTypeConverterFuncExpression()
			{
				var srcParameter = Expression.Parameter(typeof(string), "src");
				return Expression.Lambda<Func<string, string>>(
					GetTypeConverterExpression(srcParameter),
					srcParameter
				);
			}
		}

        private class CompilableIntToStringTypeConverter : ICompilableTypeConverter<int, string>
        {
            public string Convert(int src)
            {
                return src.ToString();
            }
            public Expression GetTypeConverterExpression(Expression param)
            {
                if (param == null)
                    throw new ArgumentNullException("param");
                return Expression.Call(
                    param,
                    typeof(int).GetMethod("ToString", new Type[0])
                );
            }
			public Expression<Func<int, string>> GetTypeConverterFuncExpression()
			{
				var srcParameter = Expression.Parameter(typeof(int), "src");
				return Expression.Lambda<Func<int, string>>(
					GetTypeConverterExpression(srcParameter),
					srcParameter
				);
			}
		}
    }
}
