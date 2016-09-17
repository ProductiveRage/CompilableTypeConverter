using System;
using System.Collections.Generic;

namespace UnitTesting.IntegrationTests
{
	public class ConstructorDestType
	{
		private readonly Sub1 _value;
		private readonly IEnumerable<Sub1> _valueList;
		private readonly Sub2 _valueEnum;
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

		public Sub1 Value { get { return _value; } }
		public IEnumerable<Sub1> ValueList { get { return _valueList; } }
		public Sub2 ValueEnum { get { return _valueEnum; } }

		public class Sub1
		{
			private readonly string _name;
			public Sub1(string name)
			{
				_name = name;
			}
			public string Name { get { return _name; } }
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
