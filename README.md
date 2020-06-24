# Fast mappings between types (from-and-to either mutable or immutable classes)

## A brief history lesson

This project started life because, many moons ago, AutoMapper did not have good support for mapping to immutable types. It was very happy to read property values from a source type and map them onto properties on a destination type but it was very dumb when it came to having to call a constructor on the destination type (you would have to manually map each constructor argument).

At some point (though I don't know when since I couldn't find anything in any release note), this changed and AutoMapper now does a fine job of calling constructors (and applying its magic to work out what property values are useful for providing particular constructor arguments). However, this project also mutated such that it was no longer simply an extension to AutoMapper - it could be used in isolation. It has less clever tricks than AutoMapper but it does have one big advantage, which is that it compiles is mappings down to LINQ expressions and so is very fast\*.

\* *(This isn't true any more - back in 2014, this library was about [100x faster than AutoMapper](https://www.productiverage.com/reflection-and-c-sharp-optional-constructor-arguments) but they closed the gap at some point significantly and this library is now just a curiosity, rather than a potential huge speed improvement).*

## An "in-memory" example

Below is the mapping code to translate between two types. In this case, the **SourceType** is a mutable class (it is initialised by calling a parameter-less constructor and then setting individual properties) and the **DestType** is an *immutable* class (it is initialised by passing all values into a constructor and an instance's data does not change). Mapping work in any way, though (from mutable-to-mutable, mutable-to-immutable, immutable-to-mutable or immutable-to-immutable).

	Converter.CreateMap<SourceType.Sub1, DestType.Sub1>();
	Converter.CreateMap<SourceType, DestType>();

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
	
	var dest = Converter.Convert<SourceType, DestType>(source);

This is the **SourceType** class (it has a nested sub class and enum that are handled by the mapping operation):

	public class SourceType
	{
		public Sub1 Value { get; set; }
		public IEnumerable<Sub1> ValueList { get; set; }
		public Sub2 ValueEnum { get; set; }

		public class Sub1
		{
			public string Name { get; set; }
		}

		public enum Sub2
		{
			EnumValue1,
			EnumValue2,
			EnumValue3,
			EnumValue4,
			EnumValue5,
			EnumValue6,
			EnumValue7
		}
	}
	
And here is the type that we want to map *to*:

	public class DestType
	{
		public DestType(Sub1 value, IEnumerable<Sub1> valueList, Sub2 valueEnum)
		{
			Value = value;
			ValueList = valueList;
			ValueEnum = valueEnum;
		}

		public Sub1 Value { get; }
		public IEnumerable<Sub1> ValueList { get; }
		public Sub2 ValueEnum { get; }

		public class Sub1
		{
			public Sub1(string name)
			{
				Name = name;
			}

			public string Name { get; }
		}

		public enum Sub2 : uint
		{
			EnumValue1 = 99,
			EnumValue2 = 100,
			EnumValue3 = 101,
			EnumValue4 = 102,
			EnumValue5 = 103,
			enumValue_6 = 104,
			EnumValue7 = 105
		}
	}
	
It handles collection types (as demonstrated by the mapping of the "ValueList" property in the above example).

### With ORMs

The library also has support for "projections", which are when mappings are applied to **IQueryable** references. This can allow for the simple construction of efficient database queries when using an ORM that returns **IQueryable**s. For example, the following code (which retrieves data using Entity Framework) is not be very efficient:

	// This is NOT the best way to do it
	Converter.CreateMap<Order, ImmutableOrder>();
	Converter.CreateMap<Product, ImmutableProduct>();

	ImmutableProduct[] products;
	using (var context = new NORTHWNDEntities())
	{
		products = Converter.Convert<Product, ImmutableProduct>(context.Products)
		    .ToArray();
	}

.. because "Convert" processes collections using a Select method that works with IEnumerable types, which means that all of the field that may be retrieved to delivery the "context.Products" values will be requested from the database before the mapping may be performed. Even worse, if there are any collection properties that are configured to be lazy-loaded (for example, in the code above, the class NORTHWND database is being queried and Products have Orders related to them) then you will suffer the dreade N+1 ORM performance issue.

However, if you use the alternate "ProjectionConverter", like this:

	// This way is much more efficient
	ProjectionConverter.CreateMap<Order, ImmutableOrder>();
	ProjectionConverter.CreateMap<Product, ImmutableProduct>();
	
	ImmutableProduct[] products;
	using (var context = new NORTHWNDEntities())
	{
		product = context.Products.Project().To<ImmutableProduct>()
		    .ToArray();
	}

.. then the mapping will influence the SQL query performed and only the field that are required for the final data type are retrieved. Furthermore, *all* of the data will be retrieved in one go. There will be a single query performed, rather than N+1 queries.

### Customisations

The static **Converter** class is a helper class, it's not the only way to do things. When trying to match properties / constructor arguments for a mapping, a default name matcher is used that ignores case and underscores in names (since this worked well for the code I was writing) but if you wanted different matching logic then you would to create your own converter instance. To create a converter that would translate from a mutable **SourceType** to an immutable **DestType**, as seen in the first example, you would need to do something like this:

	var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
	var converterFactory = ExtendableCompilableTypeConverterFactoryHelpers
		.GenerateConstructorBasedFactory(
			nameMatcher,
			new ArgsLengthTypeConverterPrioritiserFactory(),
			new ICompilablePropertyGetterFactory[]
			{
				new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
				new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
			},
			EnumerableSetNullHandlingOptions.ReturnNullSetForNullInput
		)
		.CreateMap<SourceType.Sub1, DestType.Sub1>()
		.CreateMap<SourceType, DestType>();
		
	var converter = converterFactory.Get<SourceType, DestType>();

The "converter" created here is different from the static **Converter** in several ways -

1. It is immutable; once a "converter" instance is created it may not be altered. If you need to support additional mappings then you need to add those more mappings to the "converterFactory" and create a new converter.
  * While this may sound like a disadvantage, there are some benefits - the static **Converter** needs to acquire a lock any time that "CreateMap" or "Convert" is called since multiple threads could be trying to alter the configuration at once, an immutable fully-configured converter instance doesn't need to lock.
  * It can also make it simpler to inject converters as dependencies - instead of a class accessing the static **Converter** reference within its code, it may have a dependency for an **ITypeConverter<TSource, TDest>**.
1. Because "GenerateConstructorBasedFactory" is called in the code above, the "converter" instance will *inly* support mapping to types that it can configure through a constructor call (ie. immutable types). If the destination type is a mutable class then "GeneratePropertySetterBasedFactory" should be called. The static **Converter** will internally try both - if constructor-based mapping fails for a particular destination type then it will attempt a property-based mapping (you could write logic to do something similar if required).

If you like the sound of being able to generate immutable type converters but you don't want to mess around with any customisations then you can use the "GetConverter" method on the static **Converter** class - eg.

	Converter.CreateMap<SourceType.Sub1, DestType.Sub1>();
	Converter.CreateMap<SourceType, DestType>();
	
	var converter = Converter.Get<SourceType, DestType>();

