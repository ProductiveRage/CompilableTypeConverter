using System;
using System.Collections.Generic;

namespace DemoApp.AutoMapperExamples
{
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
}
