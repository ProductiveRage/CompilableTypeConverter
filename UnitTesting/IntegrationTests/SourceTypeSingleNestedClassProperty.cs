namespace UnitTesting.IntegrationTests
{
	public class SourceTypeSingleNestedClassProperty
	{
		public Sub1 Value { get; set; }

		public class Sub1
		{
			public string Name { get; set; }
		}
	}
}