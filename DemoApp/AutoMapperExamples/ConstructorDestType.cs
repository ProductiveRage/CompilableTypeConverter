﻿using System;
using System.Collections.Generic;

namespace DemoApp.AutoMapperExamples
{
    public class ConstructorDestType
    {
        private Sub1 _value;
        private IEnumerable<Sub1> _valueList;
        private Sub2 _valueEnum;
        public ConstructorDestType(Sub1 value, IEnumerable<Sub1> valueList, Sub2 valueEnum)
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
        public ConstructorDestType(Sub1 value, IEnumerable<Sub1> valueList) : this(value, valueList, Sub2.EnumValue1) { }

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
}
