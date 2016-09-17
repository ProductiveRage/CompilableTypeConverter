using System.Collections.Generic;
using System.Linq;
using CompilableTypeConverter;
using CompilableTypeConverter.ConstructorPrioritisers.Factories;
using CompilableTypeConverter.NameMatchers;
using CompilableTypeConverter.PropertyGetters.Compilable;
using CompilableTypeConverter.PropertyGetters.Factories;
using CompilableTypeConverter.TypeConverters.Factories;
using NUnit.Framework;

namespace UnitTesting.IntegrationTests
{
	[TestFixture]
	public class MutableToImmutableTests
	{
		[Test]
		public void SingleStringProperty()
		{
			ConstructorDestTypeSingleStringProperty dest;
			try
			{
				Converter.CreateMap<SourceTypeSingleStringProperty, ConstructorDestTypeSingleStringProperty>();
				var source = new SourceTypeSingleStringProperty { Name = "Test" };
				dest = Converter.Convert<SourceTypeSingleStringProperty, ConstructorDestTypeSingleStringProperty>(source);
			}
			finally
			{
				Converter.Reset();
			}
			Assert.AreEqual("Test", dest.Name);
		}

		[Test]
		public void SetOfSingleStringProperty()
		{
			IEnumerable<ConstructorDestTypeSingleStringProperty> dest;
			try
			{
				Converter.CreateMap<SourceTypeSingleStringProperty, ConstructorDestTypeSingleStringProperty>();
				var source = new[]
				{
					new SourceTypeSingleStringProperty { Name = "Test1" },
					new SourceTypeSingleStringProperty { Name = "Test2" }
				};
				dest = Converter.Convert<SourceTypeSingleStringProperty, ConstructorDestTypeSingleStringProperty>(source);
			}
			finally
			{
				Converter.Reset();
			}
			Assert.AreEqual(2, dest.Count());
			Assert.AreEqual("Test1", dest.ElementAt(0).Name);
			Assert.AreEqual("Test2", dest.ElementAt(1).Name);
		}

		[Test]
		public void SingleNestedTypeProperty()
		{
			ConstructorDestTypeSingleNestedClassProperty dest;
			try
			{
				Converter.CreateMap<SourceTypeSingleNestedClassProperty.Sub1, ConstructorDestTypeSingleNestedClassProperty.Sub1>();
				Converter.CreateMap<SourceTypeSingleNestedClassProperty, ConstructorDestTypeSingleNestedClassProperty>();
				var source = new SourceTypeSingleNestedClassProperty
				{
					Value = new SourceTypeSingleNestedClassProperty.Sub1 { Name = "Test" }
				};
				dest = Converter.Convert<SourceTypeSingleNestedClassProperty, ConstructorDestTypeSingleNestedClassProperty>(source);
			}
			finally
			{
				Converter.Reset();
			}
			Assert.AreEqual("Test", dest.Value.Name);
		}

		[Test]
		public void Various()
		{
			ConstructorDestType dest;
			try
			{
				Converter.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();
				Converter.CreateMap<SourceType, ConstructorDestType>();

				var source = new SourceType
				{
					Value = new SourceType.Sub1 { Name = "Test1" },
					ValueList = new[]
					{
						new SourceType.Sub1 { Name = "Test2" },
						new SourceType.Sub1 { Name = "Test3" }
					},
					ValueEnum = SourceType.Sub2.EnumValue2
				};
				dest = Converter.Convert<SourceType, ConstructorDestType>(source);
			}
			finally
			{
				Converter.Reset();
			}
			Assert.AreEqual("Test1", dest.Value.Name);
			Assert.AreEqual(2, dest.ValueList.Count());
			Assert.AreEqual("Test2", dest.ValueList.ElementAt(0).Name);
			Assert.AreEqual("Test3", dest.ValueList.ElementAt(1).Name);
			Assert.AreEqual(ConstructorDestType.Sub2.EnumValue2, dest.ValueEnum);
		}

		[Test]
		public void VariousUsingExtendableCompilableTypeConverterFactoryHelpers()
		{
			var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
			var converterFactory =
				ExtendableCompilableTypeConverterFactoryHelpers.GenerateConstructorBasedFactory(
					nameMatcher,
					new ArgsLengthTypeConverterPrioritiserFactory(),
					new ICompilablePropertyGetterFactory[]
					{
						new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
						new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
					},
					EnumerableSetNullHandlingOptions.ReturnNullSetForNullInput
				)
				.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>()
				.CreateMap<SourceType, ConstructorDestType>();
			var converter = converterFactory.Get<SourceType, ConstructorDestType>();

			var source = new SourceType
			{
				Value = new SourceType.Sub1 { Name = "Test1" },
				ValueList = new[]
				{
					new SourceType.Sub1 { Name = "Test2" },
					new SourceType.Sub1 { Name = "Test3" }
				},
				ValueEnum = SourceType.Sub2.EnumValue2
			};
			var dest = converter.Convert(source);

			Assert.AreEqual("Test1", dest.Value.Name);
			Assert.AreEqual(2, dest.ValueList.Count());
			Assert.AreEqual("Test2", dest.ValueList.ElementAt(0).Name);
			Assert.AreEqual("Test3", dest.ValueList.ElementAt(1).Name);
			Assert.AreEqual(ConstructorDestType.Sub2.EnumValue2, dest.ValueEnum);
		}
	}
}
