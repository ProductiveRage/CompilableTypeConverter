using System;
using System.Collections.Generic;

namespace AutoMapperConstructor
{
    public class StandardDestType
    {
        public Sub1 Value { get; set; }
        public IEnumerable<Sub1> ValueList { get; set; }
        public Sub2 EnumValue { get; set; }

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
}
