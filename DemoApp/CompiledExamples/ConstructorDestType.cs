using System;
using System.Collections.Generic;

namespace DemoApp.CompiledExamples
{
    public class ConstructorDestType
    {
        private string _value;
        private IEnumerable<string> _valueList;
        private EnumSub _valueEnum;
        public ConstructorDestType(string value, IEnumerable<string> valueList, EnumSub valueEnum)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (valueList == null)
                throw new ArgumentNullException("valueList");
            if (!Enum.IsDefined(typeof(EnumSub), valueEnum))
                throw new ArgumentOutOfRangeException("valueEnum");

            _value = value;
            _valueList = valueList;
            _valueEnum = valueEnum;
        }

        public string Value { get { return _value; } }
        public IEnumerable<string > ValueList { get { return _valueList; } }
        public EnumSub ValueEnum { get { return _valueEnum; } }

        public enum EnumSub : uint
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
