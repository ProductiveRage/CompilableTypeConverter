using System.Linq;
using CompilableTypeConverter;
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
	}
}
