# Extension of AutoMapper to convert to classes with "verbose constructors"

While AutoMapper has support for translating from one type to another by passing data into the destination type's constructor, the constructor call has to be explicitly specified in the code - eg.

    Mapper.CreateMap<SourceType, DestType>().ConstructUsing(s => new DestType(s.Value1, s.Value2, s.Value3));

which means we're not taking advantage of AutoMapper's name matching or type conversions for the values we're passing into the constructor. This isn't good enough for the code I want to use AutoMapper with!

## An example

I want to be able to translate from an instance of the "SourceType" class to a new "ConstructorDestType" instance, the constructor arguments match the properties of SourceType though with different casing and their types require translation; IEnumerable<SourceType.Sub1> to IEnumerable<ConstructorDestType.Sub1> and from the SourceType.Sub2 enum to ConstructorDestType.Sub2 enum.

    // Get a no-frills, run-of-the-mill AutoMapper Configuration reference..
    var mapperConfig = new Configuration(
        new TypeMapFactory(),
        AutoMapper.Mappers.MapperRegistry.AllMappers()
    );
    mapperConfig.SourceMemberNamingConvention = new LowerUnderscoreNamingConvention();

    // .. teach it the SourceType.Sub1 to DestType.Sub1 mapping (unfortunately AutoMapper can't magically handle nested types)
    mapperConfig.CreateMap<SourceType.Sub1, ConstructorDestType.Sub1>();

    // If the translatorFactory is unable to find any constructors it can use for the conversion, the translatorFactory.Get
    // method will return null
    var translatorFactory = new SimpleTypeConverterByConstructorFactory(
        new ArgsLengthTypeConverterPrioritiser(),
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

    public class SourceType
    {
        public Sub1 Value { get; set; }
        public IEnumerable<Sub1> ValueList { get; set; }
        public Sub2 EnumValue { get; set; }

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
        private Sub2 _enumValue;
        public ConstructorDestType(Sub1 value, IEnumerable<Sub1> valueList, Sub2 enumValue)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (valueList == null)
                throw new ArgumentNullException("valueList");
            if (!Enum.IsDefined(typeof(Sub2), enumValue))
                throw new ArgumentOutOfRangeException("enumValue");
            _value = value;
            _valueList = valueList;
            _enumValue = enumValue;
        }
        public ConstructorDestType(Sub1 value, IEnumerable<Sub1> valueList) : this(value, valueList, Sub2.EnumValue1) { }

        public Sub1 Value { get { return _value; } }
        public IEnumerable<Sub1> ValueList { get { return _valueList; } }
        public Sub2 EnumValue { get { return _enumValue; } }

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

We've had to specify an IPropertyGetterFactory (AutoMapperEnabledPropertyGetterFactory) which looks for properties on the source type that can be used for the constructor arguments on the destination type, an IConstructorInvoker (SimpleConstructorInvokerFactory) which can generate a new destination type instance given a ConstructorInfo reference and set of argument values and an ITypeConverterPrioritiser (ArgsLengthTypeConverterPrioritiser) which defines what to do in cases where multiple elligible constructors are found on the destination type.

Each of these items is straight-forward and could be easily swapped out for alternate implementation if desired. I have some thoughts about using LINQ Expressions to see how far the performance can be improved over the use of reflection.

They're all passed into the SimpleTypeConverterByConstructorFactory which ties it all up and looks for constructors on the destination type whose arguments can all be fulfilled with data from source type, returning an ITypeConverterByConstructor for the most best one. This ITypeConverterByConstructor can be passed to an AutoMapper ConstructUsing method call without any arguments being specified manually, which is right where I wanted to be!

## Notes

It would be nice if we could use AutoMapper INamingConvention classes somehow as INameMatcher in this project. Not something I've looked at yet.