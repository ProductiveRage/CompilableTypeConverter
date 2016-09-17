namespace UnitTesting.IntegrationTests
{
	public class ConstructorDestTypeSingleNestedClassProperty
	{
		public ConstructorDestTypeSingleNestedClassProperty(Sub1 value)
		{
			Value = value;
		}

		public Sub1 Value { get; private set; }

		public class Sub1
		{
			public Sub1(string name)
			{
				Name = name;
			}

			public string Name { get; private set; }
		}
	}
}
