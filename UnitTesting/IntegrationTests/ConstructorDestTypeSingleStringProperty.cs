namespace UnitTesting.IntegrationTests
{
	public class ConstructorDestTypeSingleStringProperty
	{
		public ConstructorDestTypeSingleStringProperty(string name)
		{
			Name = name;
		}

		public string Name { get; private set; }
	}
}