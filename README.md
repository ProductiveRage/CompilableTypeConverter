# Extension of AutoMapper to convert to classes with "verbose constructors"

While AutoMapper has support for translating from one type to another by passing data into the destination type's constructor, the constructor call has to be explicitly specified in the code - eg.

    Mapper.CreateMap<SourceType, DestType>().ConstructUsing(
      s => new DestType(s.Value1, s.Value2, s.Value3)
    );

which means we're not taking advantage of AutoMapper's name matching or type conversions for the values we're passing into the constructor. This isn't good enough for the code I want to use AutoMapper with!

## Spoiler alert!

This readme contains details about how to set up a type converter using this library, in much the same way that you *can* configure an AutoMapper "MappingEngine" instance. But one of the charms of AutoMapper is how easy it is to get going with immediately, with no complicated configuration. To replicate this, there is a static Converter class that reduces the below example to the following:

    // Create a mapping for the nested type
    Converter.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();
    
    // Translate from an instance of SourceType into an instance of ConstructorDestType
    var dest = Converter.Convert<SourceType, ConstructorDestType>(source);

There's a brief section about this "convenience wrapper" further down.

## An example

I want to be able to translate from an instance of the "SourceType" class to a new "ConstructorDestType" instance, the constructor arguments match the properties of SourceType though with different casing and their types require translation; IEnumerable<SourceType.Sub1> to IEnumerable<ConstructorDestType.Sub1> and from the SourceType.Sub2 enum to ConstructorDestType.Sub2 enum.

    // Get a no-frills, run-of-the-mill AutoMapper Configuration reference..
    var mapperConfig = new Configuration(
        new TypeMapFactory(),
        AutoMapper.Mappers.MapperRegistry.AllMappers()
    );
    mapperConfig.SourceMemberNamingConvention = new LowerUnderscoreNamingConvention();

    // .. teach it the SourceType.Sub1 to DestType.Sub1 mapping (unfortunately
    // AutoMapper can't magically handle nested types)
    mapperConfig.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();

    // If the translatorFactory is unable to find any constructors it can use for the
    // conversion, the translatorFactory.Get method will throw an exception
    // with details of precisely what couldn't be mapped
    var translatorFactory = new SimpleTypeConverterByConstructorFactory(
        new ArgsLengthTypeConverterPrioritiserFactory(),
        new SimpleConstructorInvokerFactory(),
        new AutoMapperEnabledPropertyGetterFactory(
            new CaseInsensitiveSkipUnderscoreNameMatcher(),
            mapperConfig
        ),
		ParameterLessConstructorBehaviourOptions.Ignore
    );
    var translator = translatorFactory.Get<SourceType, ConstructorDestType>();

    // Make our translation available to the AutoMapper configuration
    mapperConfig.CreateMap<SourceType, ConstructorDestType>().ConstructUsing(
      translator.Convert
    );

    // Let AutoMapper do its thing!
    var dest = (new MappingEngine(mapperConfig)).Map<SourceType, ConstructorDestType>(
        new SourceType()
        {
            Value = new SourceType.Sub1() { Name = "Test1" },
            ValueList = new[]
            {
                new SourceType.Sub1() { Name = "Test2" },
                new SourceType.Sub1() { Name = "Test3" }
            },
            ValueEnum = SourceType.Sub2.EnumValue2
        }
    );

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

    public class ConstructorDestType
    {
        private Sub1 _value;
        private IEnumerable<Sub1> _valueList;
        private Sub2 _valueEnum;
        public ConstructorDestType(
          Sub1 value,
          IEnumerable<Sub1> valueList,
          Sub2 valueEnum)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (valueList == null)
                throw new ArgumentNullException("valueList");
            if (!Enum.IsDefined(typeof(Sub2), valueEnum))
                throw new ArgumentOutOfRangeException("valueEnum");
            _value = value;
            _valueList = valueList;
            _valueEnum = valueEnum;
        }
        public ConstructorDestType(
          Sub1 value,
          IEnumerable<Sub1> valueList)
            : this(value, valueList, Sub2.EnumValue1) { }

        public Sub1 Value { get { return _value; } }
        public IEnumerable<Sub1> ValueList { get { return _valueList; } }
        public Sub2 ValueEnum { get { return _valueEnum; } }

        public class Sub1
        {
            public string Name { get; set; }
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

## Overview

We've had to specify an IPropertyGetterFactory (AutoMapperEnabledPropertyGetterFactory) which looks for properties on the source type that can be used for the constructor arguments on the destination type, an IConstructorInvoker (SimpleConstructorInvokerFactoryt) which can generate a new destination type instance given a ConstructorInfo reference and set of argument values and an ITypeConverterPrioritiser (ArgsLengthTypeConverterPrioritiser) which defines what to do in cases where multiple elligible constructors are found on the destination type.

Each of these items is straight-forward and could be easily swapped out for alternate implementation if desired. I have some thoughts about using LINQ Expressions to see how far the performance can be improved over the use of reflection.

They're all passed into the SimpleTypeConverterByConstructorFactory which ties it all up and looks for constructors on the destination type whose arguments can all be fulfilled with data from source type, returning an ITypeConverterByConstructor for the most best one. This ITypeConverterByConstructor can be passed to an AutoMapper ConstructUsing method call without any arguments being specified manually, which is right where I wanted to be!

## Extending with LINQ Expressions - July 2011

While using AutoMapper is certainly convenient, there is an overhead which may be an issue if translating large numbers of items. As an alternative, interfaces for "compilable" property getters and type converters are available that perform the translation using LINQ Expressions and so can perform conversions comparably fast as hand-rolled alternatives. To replicate the above example, the following could be used:

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
    // - This will return null if unable to generate an appropriate converter
    var converter = converterFactory.Get<SourceType, ConstructorDestType>();
    if (converter == null)
        throw new Exception("Unable to obtain a converter");
            
    var result = converter.Convert(new SourceType()
    {
        Value = new SourceType.Sub1() { Name = "Sub1 Value1" },
        ValueList = new[]
        {
            new SourceType.Sub1() { Name = "Sub1 Value2" },
            null,
            new SourceType.Sub1() { Name = "Sub1 Value3" }
        },
        ValueEnum = SourceType.Sub2.EnumValue2
    });

This uses the ICompilablePropertyGetter, ICompilablePropertyGetterFactory, ICompilableTypeConverterByConstructor and ICompilableTypeConverterFactory interfaces. Note that AutoMapper is not used at all in this scenario.

## Property setters instead of constructors - February 2012

This project can now be used to translate back from DestType to SourceType by instantiating with a parameter-less constructor and then setting properties. The CompilableTypeConverterByPropertySettingFactory can be configured to consider it a failure (and so raise an exception) unless _all_ properties can be set from the source type or it be configured to return a converter that will set as many properties as possible (potentially zero).

There is an ExtendableCompilableTypeConverterFactoryHelpers method such that an example very similar to the previous one can be constructed:

    var nameMatcher = new CaseInsensitiveSkipUnderscoreNameMatcher();
    var converterFactory = ExtendableCompilableTypeConverterFactoryHelpers.GeneratePropertySetterBasedFactory(
        nameMatcher,
        CompilableTypeConverterByPropertySettingFactory.PropertySettingTypeOptions.MatchAll,
        new ICompilablePropertyGetterFactory[]
        {
            new CompilableAssignableTypesPropertyGetterFactory(nameMatcher),
            new CompilableEnumConversionPropertyGetterFactory(nameMatcher)
        },
        new PropertyInfo[0], // propertiesToIgnore
        ByPropertySettingNullSourceBehaviourOptions.UseDestDefaultIfSourceIsNull,
        new PropertyInfo[0], // initialisedFlagsIfTranslatingNullsToEmptyInstances
        EnumerableSetNullHandlingOptions.ReturnNullSetForNullInput
    );

    converterFactory = converterFactory.CreateMap<ConstructorDestType.Sub1, SourceType.Sub1>();

    var converter = converterFactory.Get<ConstructorDestType, SourceType>();
    var result = converter.Convert(
        new ConstructorDestType(
            new ConstructorDestType.Sub1("Sub1 Value1"),
            new[]
            {
                new ConstructorDestType.Sub1("Sub1 Value2"),
                null,
                new ConstructorDestType.Sub1("Sub1 Value3")
            },
            ConstructorDestType.Sub2.EnumValue2
        )
    );

It may seem like this has come back full circle now since AutoMapper is very capable of these sorts of conversions - in fact _more_ capable in some cases due to a larger set of conversions available out of the box - but it was bugging me that the project here could only translate one way (TO constructor-based types). This also has the benefit of making use of LINQ Expressions and so should be significantly quicker than AutoMapper when converting many instances of the same types.

## The "Converter" convenience wrapper - January 2014

One of the issues with the above examples is how much setup code is required to prepare each converter. This is something that I've addressed with the static "Converter" class for the common use cases.

The first method is "CreateMap". This takes two type parameters, for the source type and destination type. It will throw an exception if the mapping could not be generated.

Any mapping generated through a call to this method will expand the converter's repertoire of possible operations. In the example code above, there were mappings generated from type "SourceType" to type "ConstructorDestType". In order to perform this mapping, there must be a mapping generated from "SourceType.Sub1" to "ConstructorDestType.Sub1". This could be configured by the following -

    Converter.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();
    Converter.CreateMap<SourceType, ConstructorDestType>();

When the Converter creates the type mappings - such as SourceType.Sub1 to ConstructorDestType.Sub1 - it also generates mappings for sets of these types, so the ValueList can be mapped from SourceType to the ConstructorDestType's constructor argument.

The Converter also has support enabled for enum mappings, based upon the LowerUnderscoreNamingConvention class, so a CreateMap was not necessary for translating SourceType.Sub2 to ConstructorDestType.Sub2 (where both Sub2 types were enums).

Once mappings has been configured, the "Convert" method may be used to take advantage of them - eg.

    var dest = Converter.Convert<SourceType, ConstructorDestType>(source);

If there is no mapping available for the requested translation, then the Converter will try to generate one in the Convert call. An exception will be thrown if no mapping could be created or if the mapping failed (eg. an exception was thrown by the constructor of the destination type, if a by-constructor mapping was performed).

Since Convert calls will try to generate the required mapping if necessary, the above example could be reduced to

    Converter.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();
    var dest = Converter.Convert<SourceType, ConstructorDestType>(source);

No explicit "CreateMap&lt;SourceType, ConstructorDestType>&gt;" call required!

The mappings generated by the Converter may by "by-constructor" or "by-property-setter". It will first attempt to map "by-constructor" if there are non-parameter-less-constructors on the destination type. If it is unable to do so then it will attempt a by-property-setter mapping. In order to do this, a public parameter-less constructor must be present on the destination type and all of the public (non-indexed) settable properties on the destination type must be mappable from the source type.

If a by-property-setter mapping is desired that shouldn't try to map *all* properties on the destination type (if, for example, only a subset of them are available on the source type) then mapping exceptions may be specified with the "BeginCreateMap" method - eg.

    Converter.BeginCreateMap<SourceType, DestType>()
        .Ignore(
            d => d.Name
        )
        .Create();

The type converters in the library are all immutable. The Converter wrapper, however, is not. This makes its interface easier to get going with and for many simple use cases. The downsides are that this potentially means that a call to "Convert" now may execute different code to a call to "Convert" later, even for the same mapping (since the Converter may have had its configuration altered by code elsewhere). All of its methods also implement locking to ensure that calling this static class is thread-safe and this has an (admittedly marginal) performance impact.

Both of these issues can be avoided by use of the method "GetConverter". This will return an immutable converter that will be unaffected by any further changes to the Converter's configuration and will not require any locks around its access.

    var converter = Converter.GetConverter<SourceType, ConstructorDestType>();

As with the other methods, if a translation for the requested mapping could not generated then an exception will be thrown.

Finally, there is a "Reset" method that will revert the Converter back to its initial state, all mappings that it has built up through use will be forgotten. This may mean that calls to "Convert" that worked earlier now throw exceptions.

The last thing to mention in regard to this wrapper class is that every converter that it generates use compiled LINQ Expressions - so everything that you use here should be nice and quick! With sensible use of the "GetConverter" method, you should be able to get near the performance of hand-written translation code!

Rule of thumb: There is an overhead to the generate and compliation of these expression-based converters but if you're intending to perform more than a few hundred translations using any given converter then this upfront time is likely offset.